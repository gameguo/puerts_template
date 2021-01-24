using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Puerts
{
    public class HotfixEditor
    {
        [PostProcessScene]
        [MenuItem("Puerts/Inject")]
        public static void RunInject()
        {
            if (UnityEngine.Application.isPlaying) return;
            if (EditorApplication.isCompiling)
            {
                UnityEngine.Debug.LogError("compiling...");
                return;
            }
            EditorUtility.DisplayProgressBar("Inject", "injecting...", 0);
            try { InjectAssembly(); }
            catch (Exception e) {  UnityEngine.Debug.LogError(e); }
            EditorUtility.ClearProgressBar();
        }
        [MenuItem("Puerts/Inject Clear (Script Compilation)")]
        public static void InjectClear()
        {
            UnityEditorUtility.Compilation(); // 重新编译脚本
        }
        /// <summary> 对 Assembly-CSharp 注入 </summary>
        public static void InjectAssembly()
        {
            if (EditorApplication.isCompiling || UnityEngine.Application.isPlaying) return;
            InjectAssembly("Assembly-CSharp");
            AssetDatabase.Refresh();
        }
        /// <summary> 对指定的程序集注入 </summary>
        /// <param name="assembly">程序集路径</param>
        public static void InjectAssembly(string assembly)
        {
            var configure = GetConfig();
            var filters = GetFilters();
            var getHotfixConfig = GetHotfixConfig();

            var injectList = new List<string>();
            foreach (var kv in configure) // 所有config
            {
                // 过滤出所有指定程序集的Type
                var typeList = kv.Value.Where(item => item.Key is Type)
                    .Select(item => new KeyValuePair<Type, int>(item.Key as Type, item.Value))
                    .Where(item => item.Key.Assembly.GetName().Name == assembly)
                    .ToList();
                foreach (var cfgItem in typeList)
                {
                    if (cfgItem.Key.IsInterface) continue; // 跳过接口
                    var config = getHotfixConfig == null ? GetHotfixConfig(cfgItem.Key) : (HotfixConfig)getHotfixConfig.Invoke(null, new object[] { cfgItem.Key });

                    foreach (var method in cfgItem.Key.GetMethods(GetHotfixMethodType()))
                    {
                        if (IsHotfix(method, filters, config))
                        {
                            injectList.Add(GetMethodString(method));
                        }
                    }
                }
            }
            if (injectList.Count >= 0)
            {
                var assembly_path = string.Format("./Library/{0}/{1}.dll", GetScriptAssembliesFolder(), assembly);
                HotfixInject.StartInject(assembly_path, injectList);
            }
            else
            {
                UnityEngine.Debug.LogWarning("Jump Inject");
            }
        }

        /// <summary> 是否需要Hotfix </summary>
        #region IsHotfix
        private static bool IsHotfix(MethodInfo method, List<MethodInfo> filters, HotfixConfig config)
        {
            if (config.ignoreNotPublic && method.IsPublic) return false;
            if (method.IsAbstract) return false;
            if (filters == null || filters.Count == 0) return true;
            foreach (var filter in filters)
            {
                if ((bool)filter.Invoke(null, new object[] { method }))
                {
                    return false;
                }
            }
            return true;
        } 
        #endregion

        #region GetConfig
        /// <summary> 获取Config </summary>
        public static Dictionary<string, List<KeyValuePair<object, int>>> GetConfig()
        {
            string hotfixAttribute = "Puerts.HotfixListAttribute";
            return Configure.GetConfigureByTags(new List<string> { hotfixAttribute });
        }
        /// <summary> 获取过滤方法List </summary>
        public static List<MethodInfo> GetFilters()
        {
            var types = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                        where !(assembly.ManifestModule is System.Reflection.Emit.ModuleBuilder)
                        from type in assembly.GetTypes()
                        where type.IsDefined(typeof(ConfigureAttribute), false)
                        select type;

            List<MethodInfo> filters = new List<MethodInfo>();
            foreach (var type in types)
            {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (method.IsDefined(typeof(HotfixFilterAttribute), false))
                    {
                        filters.Add(method);
                    }
                }
            }
            return filters;
        }
        /// <summary> 获取 HotfixConfig的方法 </summary>
        public static MethodInfo GetHotfixConfig()
        {
            var types = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                        where !(assembly.ManifestModule is System.Reflection.Emit.ModuleBuilder)
                        from type in assembly.GetTypes()
                        where type.IsDefined(typeof(ConfigureAttribute), false)
                        select type;
            foreach (var type in types)
            {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (method.IsDefined(typeof(HotfixConfigAttribute), false))
                    {
                        return method;
                    }
                }
            }
            return null;
        }
        private static HotfixConfig GetHotfixConfig(Type type)
        {
            return HotfixConfig.GetDefault();
        }
        /// <summary> 获取所有需要Hotfix的方法类型 </summary>
        private static BindingFlags GetHotfixMethodType()
        {
            return BindingFlags.Instance
                 | BindingFlags.Static | BindingFlags.Public
                 | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        }
        /// <summary> 获取程序集所在文件夹 </summary>
        private static string GetScriptAssembliesFolder()
        {
            var assembliesFolder = "PlayerScriptAssemblies";
            if (!Directory.Exists(string.Format("./Library/{0}/", assembliesFolder)))
            {
                assembliesFolder = "ScriptAssemblies";
            }
            return assembliesFolder;
        }
        #endregion

        #region GetMethodString
        private static string GetMethodString(MethodInfo method)
        {
            // Type MethodName(Type parme1Name,Type parme2Name);
            return string.Format("{0} {1}.{2}({3});",
                GetCecilTypeName(method.ReturnType),
                GetCecilTypeName(method.DeclaringType),
                method.Name,
                GetMethodParamsString(method));
        }
        private static string GetMethodParamsString(MethodInfo method)
        {
            // Type parme1Name,Type parme2Name
            var result = "";
            var parameters = method.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                result += string.Format("{0} {1}", GetCecilTypeName(p.ParameterType), p.Name);
                if (i != parameters.Length - 1)
                {
                    result += ", ";
                }
            }
            return result;
        }
        /// <summary> cecil里的类名表示和.net标准并不一样，这里做些转换 </summary>
        private static string GetCecilTypeName(Type type)
        {
            if (type.IsByRef && type.GetElementType().IsGenericType)
            {
                return GetCecilTypeName(type.GetElementType()) + "&";
            }
            else if (type.IsGenericType)
            {
                if (type.IsGenericTypeDefinition)
                {
                    return type.ToString().Replace('+', '/').Replace('[', '<').Replace(']', '>');
                }
                else
                {
                    return System.Text.RegularExpressions.Regex.Replace(type.ToString().Replace('+', '/'), @"(`\d).+", "$1")
                        + "<" + string.Join(",", type.GetGenericArguments().Select(t => GetCecilTypeName(t))
                        .ToArray()) + ">";
                }
            }
            else
            {
                return type.FullName.Replace('+', '/');
            }
        }
        #endregion
    }
}

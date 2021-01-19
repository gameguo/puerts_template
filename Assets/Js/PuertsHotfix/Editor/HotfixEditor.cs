using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Puerts
{
    public class HotfixEditor
    {
        [MenuItem("Puerts/Inject")]
        public static void RunInject()
        {
            if (EditorApplication.isCompiling || UnityEngine.Application.isPlaying)
            {
                UnityEngine.Debug.LogError("compiling or playing");
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
            string hotfixAttribute = "Puerts.HotfixListAttribute";
            var configure = Configure.GetConfigureByTags(new List<string> { hotfixAttribute });
            var filters = GetFilters();

            var injectList = new List<MethodInfo>();
            foreach (var kv in configure) // 所有config
            {
                // 过滤出所有指定程序集的Type
                var typeList = kv.Value.Where(item => item.Key is Type)
                    .Select(item => new KeyValuePair<Type, int>(item.Key as Type, item.Value))
                    .Where(item => item.Key.Assembly.GetName().Name == assembly)
                    .ToList();
                foreach (var cfgItem in typeList)
                {
                    foreach (var method in cfgItem.Key.GetMethods(GetHotfixMethodType()))
                    {
                        if (IsHotfix(method, filters))
                        {
                            injectList.Add(method);
                        }
                    }
                }
            }
            if (injectList.Count >= 0)
            {
                var assembly_path = string.Format("./Library/{0}/{1}.dll", GetScriptAssembliesFolder(), assembly);
                InjectUtility.StartInject(assembly_path, injectList);
            }
            else
            {
                UnityEngine.Debug.LogWarning("Jump Inject");
            }
        }

        #region Tools
        /// <summary> 获取所有需要Hotfix的方法类型 </summary>
        private static BindingFlags GetHotfixMethodType()
        {
            return BindingFlags.Instance
                 | BindingFlags.Static | BindingFlags.Public
                 | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        }
        /// <summary> 是否需要Hotfix </summary>
        private static bool IsHotfix(MethodInfo method, List<MethodInfo> filters)
        {
            if (filters == null || filters.Count == 0) return true;
            foreach (var filter in filters)
            {
                if ((bool)filter.Invoke(null, new object[] { method}))
                {
                    return false;
                }
            }
            return true;
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
        /// <summary> 获取过滤List </summary>
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
        #endregion
    }
}

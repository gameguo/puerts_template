using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Puerts
{
    public class PuertsHotfixEditor
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
            var configure = Configure.GetConfigureByTags(new List<string> { "Puerts.HotfixListAttribute" });

            var filters = GetFilters();

            var processCfgPath = "./process_cfg";

            // 该程序集是否有配置了些类，如果没有就跳过注入操作
            bool hasSomethingToDo = false;

            var blackList = new List<MethodInfo>();

            using (BinaryWriter writer = new BinaryWriter(new FileStream(processCfgPath, FileMode.Create,
                FileAccess.Write)))
            {
                writer.Write(configure.Count);

                foreach (var kv in configure)
                {
                    writer.Write(kv.Key);

                    var typeList = kv.Value.Where(item => item.Key is Type)
                        .Select(item => new KeyValuePair<Type, int>(item.Key as Type, item.Value))
                        .Where(item => item.Key.Assembly.GetName().Name == assembly)
                        .ToList();
                    writer.Write(typeList.Count);

                    if (typeList.Count > 0)
                    {
                        hasSomethingToDo = true;
                    }

                    foreach (var cfgItem in typeList)
                    {
                        writer.Write(GetCecilTypeName(cfgItem.Key));
                        writer.Write(cfgItem.Value);
                        if (filters.Count > 0 && kv.Key == "IFix.IFixAttribute")
                        {
                            foreach (var method in cfgItem.Key.GetMethods(BindingFlags.Instance
                                | BindingFlags.Static | BindingFlags.Public
                                | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                            {
                                foreach (var filter in filters)
                                {
                                    if ((bool)filter.Invoke(null, new object[]
                                    {
                                        method
                                    }))
                                    {
                                        blackList.Add(method);
                                    }
                                }
                            }
                        }
                    }
                }

                writeMethods(writer, blackList);
            }

            if (hasSomethingToDo)
            {
                var assembly_path = string.Format("./Library/{0}/{1}.dll", GetScriptAssembliesFolder(), assembly);
                var pathList = new List<string>();
                foreach (var path in
                    (from asm in AppDomain.CurrentDomain.GetAssemblies()
                     select Path.GetDirectoryName(asm.ManifestModule.FullyQualifiedName)).Distinct())
                {
                    try { pathList.Add(path); }
                    catch { }
                }
                InjectUtility.StartInject(processCfgPath, assembly_path, assembly_path, pathList);
            }
            else
            {
                UnityEngine.Debug.LogWarning("Jump Inject");
            }

            File.Delete(processCfgPath);
        }

        #region Tools
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
        /// <summary> cecil里的类名表示和.net标准并不一样，这里做些转换 </summary>
        static string GetCecilTypeName(Type type)
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
        /// <summary> 
        /// 把方法签名写入文件
        /// 由于目前不支持泛型函数的patch，所以函数签名为方法名+参数类型 
        /// </summary>
        static void writeMethods(BinaryWriter writer, List<MethodInfo> methods)
        {
            var methodGroups = methods.GroupBy(m => m.DeclaringType).ToList();
            writer.Write(methodGroups.Count);
            foreach (var methodGroup in methodGroups)
            {
                writer.Write(GetCecilTypeName(methodGroup.Key));
                writer.Write(methodGroup.Count());
                foreach (var method in methodGroup)
                {
                    writer.Write(method.Name);
                    writer.Write(GetCecilTypeName(method.ReturnType));
                    writer.Write(method.GetParameters().Length);
                    foreach (var parameter in method.GetParameters())
                    {
                        writer.Write(parameter.IsOut);
                        writer.Write(GetCecilTypeName(parameter.ParameterType));
                    }
                }
            }
        }
        #endregion

        //private static bool IsHotfixType(TypeDefinition td, IEnumerable<Type> types)
        //{
        //    foreach (var type in types)
        //    {
        //        if (td.FullName == type.FullName)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}
        //public static bool IsDirty(AssemblyDefinition a)
        //{
        //    foreach (var type in a.MainModule.Types)
        //    {
        //        if (type.Name == TypeNameForInjectFlag)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}
        //private const string TypeNameForInjectFlag = "_puerts_injected_flag_";
        //public static void Run()
        //{
        //    // 读取 Assembly-CSharp 程序集
        //    var testAssembly = System.Reflection.Assembly.Load("Assembly-CSharp");
        //    var assemblyFilePath = testAssembly.Location;
        //    var a = AssemblyDefinition.ReadAssembly(assemblyFilePath);
        //    var modified = false;

        //    if (!IsDirty(a))
        //    {
        //        Debug.LogError("dirty dll"); // dll已执行过修复
        //        return;
        //    }

        //    var configure = Configure.GetConfigureByTags(new List<string>{"Puerts.HotfixListAttribute"});
        //    var hotfixTypes = configure["Puerts.HotfixListAttribute"].Select(kv => kv.Key)
        //        .Where(o => o is Type)
        //        .Cast<Type>()
        //        .Where(t => !t.IsGenericTypeDefinition);

        //    foreach (var type in a.MainModule.Types)
        //    {
        //        if (!IsHotfixType(type, hotfixTypes))
        //        {
        //            continue;
        //        }
        //        var sb = $"{type.FullName}\n";
        //        foreach (var method in type.Methods)
        //        {
        //            var result = InjectMethod(a.MainModule, method);
        //            if (!string.IsNullOrEmpty(result))
        //            {
        //                modified = true;
        //                sb += result;
        //            }
        //        }
        //        Debug.LogFormat("{0}", sb);
        //    }

        //    if (modified)
        //    {
        //        a.MainModule.Types.Add(new TypeDefinition("Puerts", TypeNameForInjectFlag, TypeAttributes.Class, a.MainModule.TypeSystem.Object));
        //        a.Write(assemblyFilePath);
        //        Debug.LogFormat("write: {0}", assemblyFilePath);
        //    }
        //    else
        //    {
        //        Debug.LogWarningFormat("no change");
        //    }
        //}



        //private static string InjectMethod(ModuleDefinition module, MethodDefinition method)
        //{
        //    //if (method.IsConstructor || method.IsGetter || method.IsSetter || !method.IsPublic)
        //    //    continue;


        //    //// 定义稍后会用的类型
        //    //var objType = module.ImportReference(typeof(System.Object));
        //    //var intType = module.ImportReference(typeof(System.Int32));
        //    //var logFormatMethod =
        //    //    module.ImportReference(typeof(Debug).GetMethod("LogFormat", new[] { typeof(string), typeof(object[]) }));

        //    //// 开始注入IL代码
        //    //var insertPoint = method.Body.Instructions[0];
        //    //var ilProcessor = method.Body.GetILProcessor();
        //    //// 设置一些标签用于语句跳转
        //    //var label1 = ilProcessor.Create(OpCodes.Ldarg_1);
        //    //var label2 = ilProcessor.Create(OpCodes.Stloc_0);
        //    //var label3 = ilProcessor.Create(OpCodes.Ldloc_0);
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Nop));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Ldstr, "a = {0}, b = {1}"));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Ldc_I4_2));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Newarr, objType));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Dup));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Ldc_I4_0));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Ldarg_0));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Box, intType));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Stelem_Ref));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Dup));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Ldc_I4_1));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Ldarg_1));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Box, intType));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Stelem_Ref));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Call, logFormatMethod));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Ldarg_0));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Ldarg_1));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Ble, label1));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Ldarg_0));
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Br, label2));
        //    //ilProcessor.InsertBefore(insertPoint, label1);
        //    //ilProcessor.InsertBefore(insertPoint, label2);
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Br, label3));
        //    //ilProcessor.InsertBefore(insertPoint, label3);
        //    //ilProcessor.InsertBefore(insertPoint, ilProcessor.Create(OpCodes.Ret));


        //    return "";
        //}


        //public static string GetMethodString(MethodDefinition method)
        //{
        //    var sb = "";
        //    sb += $"{method.ReturnType} ";
        //    sb += $"{method.DeclaringType.FullName}.";
        //    sb += $"{method.Name}(";
        //    for (var i = 0; i < method.Parameters.Count; i++)
        //    {
        //        var p = method.Parameters[i];
        //        sb += $"{p.ParameterType} {p.Name}";
        //        if (i != method.Parameters.Count - 1)
        //        {
        //            sb += ", ";
        //        }
        //    }
        //    sb += ");";

        //    return sb;
        //}
    }
}

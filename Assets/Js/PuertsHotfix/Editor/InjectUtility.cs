using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Puerts
{
    public static class InjectUtility
    {
        /// <summary> 开始注入 </summary>
        public static void StartInject(string assmeblyPath, List<MethodInfo> injectList)
        {
            AssemblyDefinition assembly = null;
            try
            {
                assembly = AssemblyDefinition.ReadAssembly(assmeblyPath,
                    new ReaderParameters { ReadSymbols = true, ReadWrite = true });

                if (IsDirty(assembly))
                {
                    UnityEngine.Debug.LogError("assembly dirty");
                    return;
                }

                Inject(assembly, injectList); // 注入

                SetDirty(assembly);
                assembly.Write(new WriterParameters{ WriteSymbols = true });
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Inject Exception:\r\n" + e);
                return;
            }
            finally
            {
                //如果不清理，在window下会锁定文件
                if (assembly != null && assembly.MainModule.SymbolReader != null)
                {
                    assembly.MainModule.SymbolReader.Dispose();
                }
                if (assembly != null)
                {
                    assembly.Dispose();
                }
            }
            UnityEngine.Debug.Log(Path.GetFileName(assmeblyPath) + " inject success");
        }
        /// <summary> 注入 </summary>
        private static void Inject(AssemblyDefinition assembly, List<MethodInfo> injectList)
        {
            var module = assembly.MainModule;

            UnityEngine.Debug.Log(injectList.Count);

            foreach (var type in module.Types)
            {
                var sb = $"{type.FullName}\n";
                foreach (var method in type.Methods)
                {
                    if (!IsHotfix(method, injectList)) continue;

                    var result = InjectMethod(module, method);
                    if (!string.IsNullOrEmpty(result))
                    {
                        sb += result + "\n";
                    }
                }
                UnityEngine.Debug.LogFormat("{0}", sb);
            }
        }

        private static string InjectMethod(ModuleDefinition module, MethodDefinition mothod)
        {

            return "";
        }

        private static bool IsHotfix(MethodDefinition method, List<MethodInfo> injectList)
        {
            var methodClassType = method.DeclaringType.FullName;
            var methodName = method.Name;
            var methodReturnType = method.ReturnType.FullName;
            int methodParameterCount = method.HasParameters ? method.Parameters.Count : 0;
            var parames = new List<MethodParameterStr>();

            foreach (var parameter in method.Parameters)
            {
                //parameter.IsReturnValue
                //var p = new MethodParameterStr()
                //{

                //};
                //parames.Add(p);
                //writer.Write(parameter.IsOut);
                //writer.Write(GetCecilTypeName(parameter.ParameterType));
            }


            UnityEngine.Debug.Log(methodClassType);

            foreach (var item in injectList)
            {
                var classType = GetCecilTypeName(item.DeclaringType);
                if (classType == methodClassType)
                {
                    
                }
            }
            return false;
        }

        public struct MethodParameterStr
        {

        }

        #region Dirty
        private const string TypeNameForInjectFlag = "_puerts_injected_flag_";
        public static bool IsDirty(AssemblyDefinition a)
        {
            foreach (var type in a.MainModule.Types)
            {
                if (type.Name == TypeNameForInjectFlag)
                {
                    return true;
                }
            }
            return false;
        }
        public static void SetDirty(AssemblyDefinition a)
        {
            a.MainModule.Types.Add(
                new TypeDefinition("Puerts", TypeNameForInjectFlag,
                Mono.Cecil.TypeAttributes.Class, a.MainModule.TypeSystem.Object));
        }
        #endregion

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
        static void GetMethods(List<MethodInfo> methods)
        {
            //var methodGroups = methods.GroupBy(m => m.DeclaringType).ToList();
            //writer.Write(methodGroups.Count);
            //foreach (var methodGroup in methodGroups)
            //{
            //    writer.Write(GetCecilTypeName(methodGroup.Key));
            //    writer.Write(methodGroup.Count());
            //    foreach (var method in methodGroup)
            //    {
            //        writer.Write(method.Name);
            //        writer.Write(GetCecilTypeName(method.ReturnType));
            //        writer.Write(method.GetParameters().Length);
            //        foreach (var parameter in method.GetParameters())
            //        {
            //            writer.Write(parameter.IsOut);
            //            writer.Write(GetCecilTypeName(parameter.ParameterType));
            //        }
            //    }
            //}
        }
    }
}

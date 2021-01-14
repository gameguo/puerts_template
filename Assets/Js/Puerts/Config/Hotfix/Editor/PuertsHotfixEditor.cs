using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEngine;

namespace Puerts
{
    public class PuertsHotfixEditor
    {
        private const string TypeNameForInjectFlag = "_puerts_injected_flag_";

        [MenuItem("Puerts/Hotfix")]
        public static void RunHotfix()
        {
            Run();
        }

        private static bool IsHotfixTarget(TypeDefinition td, IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                if (td.FullName == type.FullName)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsFlag(AssemblyDefinition a)
        {
            foreach (var type in a.MainModule.Types)
            {
                if (type.Name == TypeNameForInjectFlag)
                {
                    return false;
                }
            }
            return true;
        }

        #region GetDelegate
        public static bool IsParameterMatched(ParameterDefinition p1, ParameterDefinition p2)
        {
            return p1.ParameterType == p2.ParameterType && p1.IsOut == p2.IsOut;
        }

        // 方法定义是否与 hotfix 委托定义匹配
        public static bool IsDelegateMatched(MethodDefinition m, TypeReference returnType, TypeDefinition d)
        {
            var invoke = d.Methods.First(dm => dm.Name == "Invoke");
            var argc = invoke.Parameters.Count;
            if (argc != m.Parameters.Count + 1)
            {
                return false;
            }

            if (invoke.ReturnType != returnType)
            {
                return false;
            }

            if (invoke.Parameters[0].IsOut)
            {
                return false;
            }

            if (m.IsStatic)
            {
                if (invoke.Parameters[0].ParameterType.FullName != "System.Type")
                {
                    return false;
                }
            }
            else
            {
                if (invoke.Parameters[0].ParameterType.FullName != "System.Object")
                {
                    return false;
                }
            }

            for (var i = 1; i < argc; i++)
            {
                var p1 = invoke.Parameters[i];
                var p2 = m.Parameters[i - 1];

                if (!IsParameterMatched(p1, p2))
                {
                    return false;
                }
            }

            return true;
        }

        // 从 Delegate 定义池中找一个匹配的
        public static TypeDefinition GetDelegate(MethodDefinition m, TypeReference returnType, List<TypeDefinition> list)
        {
            if (m.Name != ".cctor")
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if (IsDelegateMatched(m, returnType, item))
                    {
                        return item;
                    }
                }
            }

            return null;
        }

        #endregion

        public static string GetMethodString(MethodDefinition method)
        {
            var sb = "";
            sb += $"{method.ReturnType} ";
            sb += $"{method.DeclaringType.FullName}.";
            sb += $"{method.Name}(";
            for (var i = 0; i < method.Parameters.Count; i++)
            {
                var p = method.Parameters[i];
                sb += $"{p.ParameterType} {p.Name}";
                if (i != method.Parameters.Count - 1)
                {
                    sb += ", ";
                }
            }
            sb += ");";

            return sb;
        }

        private static OpCode[] ldarg_i_table = new OpCode[] { OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3 };

        private static Instruction FindPatchPoint(MethodBody body)
        {
            var instructions = body.Instructions;
            return instructions.Count > 0 ? instructions[0] : null;
        }

        private static string GetHotfixFieldName_r(MethodDefinition method, HashSet<string> set)
        {
            var plainName = method.IsConstructor ? "_JSFIX_RC_" + method.Name.Replace(".", "") : "_JSFIX_R_" + method.Name;
            var index = 0;
            var serialName = plainName + "_" + index;

            while (set.Contains(serialName))
            {
                serialName = plainName + "_" + ++index;
            }

            set.Add(serialName);
            return serialName;
        }

        private static string GetHotfixFieldName_b(MethodDefinition method, HashSet<string> set)
        {
            var plainName = method.IsConstructor ? "_JSFIX_BC_" + method.Name.Replace(".", "") : "_JSFIX_B_" + method.Name;
            var index = 0;
            var serialName = plainName + "_" + index;

            while (set.Contains(serialName))
            {
                serialName = plainName + "_" + ++index;
            }

            return serialName;
        }

        public static void Run()
        {
            // 读取 Assembly-CSharp 程序集
            var testAssembly = System.Reflection.Assembly.Load("Assembly-CSharp");
            var assemblyFilePath = testAssembly.Location;
            var a = AssemblyDefinition.ReadAssembly(assemblyFilePath);
            var modified = false;

            if (!IsFlag(a))
            {
                Debug.LogError("dirty dll"); // dll已执行过修复
                return;
            }

            var configure = Configure.GetConfigureByTags(new List<string>{"Puerts.HotfixListAttribute"});
            var hotfixTypes = configure["Puerts.HotfixListAttribute"].Select(kv => kv.Key)
                .Where(o => o is Type)
                .Cast<Type>()
                .Where(t => !t.IsGenericTypeDefinition);

            foreach (var type in a.MainModule.Types)
            {
                if (!IsHotfixTarget(type, hotfixTypes))
                {
                    continue;
                }
                var sb = $"{type.FullName}\n";
                var hotfixRegs = new HashSet<string>();
                foreach (var method in type.Methods)
                {
                    var hotfixFieldName_r = GetHotfixFieldName_r(method, hotfixRegs);
                    var signatureLit = GetMethodString(method);

                    var point = FindPatchPoint(method.Body);
                    if (point == null)
                    {
                        Debug.LogWarningFormat("no patch point in {0}", signatureLit);
                        continue;
                    }

                    modified = true;
                    var argCount = method.IsStatic ? method.Parameters.Count : method.Parameters.Count + 1;
                    var proc = method.Body.GetILProcessor();
                    var boolVar = new VariableDefinition(a.MainModule.TypeSystem.Boolean);
                    method.Body.Variables.Add(boolVar);

                    sb += hotfixFieldName_r + " > " + signatureLit;
                    sb += "\n";
                }
                Debug.LogFormat("{0}", sb);
            }

            if (modified)
            {
                a.MainModule.Types.Add(new TypeDefinition("Puerts", TypeNameForInjectFlag, Mono.Cecil.TypeAttributes.Class, a.MainModule.TypeSystem.Object));
                a.Write(assemblyFilePath);
                // a.Write("temp.dll");
                Debug.LogFormat("write: {0}", assemblyFilePath);
            }
            else
            {
                Debug.LogWarningFormat("no change");
            }
        }
    }
}

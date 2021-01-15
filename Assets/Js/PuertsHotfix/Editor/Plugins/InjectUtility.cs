using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;

namespace Puerts
{
    public static class InjectUtility
    {
        public static void StartInject(string processCfgPath, string assmeblyPath, string assemblyOutputPath, List<string> paths)
        {
            AssemblyDefinition assembly = null;
            AssemblyDefinition ilfixAassembly = null;
            var tranlater = new CodeTranslator();
            string dllName = null;
            try
            {
                bool readSymbols = true;
                try
                {
                    //尝试读取符号
                    assembly = AssemblyDefinition.ReadAssembly(assmeblyPath,
                        new ReaderParameters { ReadSymbols = true });
                }
                catch (Exception e)
                {
                    /*
                     * if isWin:
                     * currentPath + "/precompile", targetName + ".dll", "-o:" + targetName + ".AOT.dll", "-t:AOTDataConfig" ], shell = True, cwd = targetPath)
                     * else:
                     * p = subprocess.Popen(["/Library/Frameworks/Mono.framework/Versions/Current/bin/mono", currentPath + "/precompile.exe", targetName + ".dll", "-o:" + targetName + ".AOT.dll", "-t:AOTDataConfig" ], shell = False, cwd = targetPath)
                     */
                    // "E:\DevelopmentSoft\Unitys\2018.4.27f1\Unity\Editor\Data\MonoBleedingEdge/bin/mono" "E:\DevelopmentSoft\Unitys\2018.4.27f1\Unity\Editor\Data\MonoBleedingEdge\lib\mono\4.5\pdb2mdb.exe" "G:\Unity\Git\puerts_template\Library\ScriptAssemblies\Assembly-CSharp.dll"
                    //"E:\DevelopmentSoft\Unitys\2018.4.27f1\Unity\Editor\Data\MonoBleedingEdge\bin\mono.exe" "E:\DevelopmentSoft\Unitys\2018.4.27f1\Unity\Editor\Data\MonoBleedingEdge\lib\mono\4.5\mcs.exe"  "G:\Unity\Git\puerts_template\Library\ScriptAssemblies\Assembly-CSharp.dll"
                    //如果读取不到符号则不读
                    UnityEngine.Debug.LogWarning(assmeblyPath + " with symbol fail\n" + e);
                    //写入的时候用这个标志
                    readSymbols = false;
                    assembly = AssemblyDefinition.ReadAssembly(assmeblyPath,
                        new ReaderParameters { ReadSymbols = false });
                }

                var resolver = assembly.MainModule.AssemblyResolver as BaseAssemblyResolver;
                //往resolver加入程序集搜索路径
                foreach (var path in paths)
                {
                    // UnityEngine.Debug.Log("searchPath:" + path);
                    try { resolver.AddSearchDirectory(path); } catch { }
                }
                dllName = Path.GetFileName(assmeblyPath);
                GenerateConfigure configure = null;
                configure = GenerateConfigure.FromFile(processCfgPath);

                ilfixAassembly = AssemblyDefinition.ReadAssembly("./Assets/Js/PuertsHotfix/IFix/Plugins/IFix.Core.dll");
                //注入逻辑
                if (tranlater.Process(assembly, ilfixAassembly, configure, ProcessMode.Inject)
                    == CodeTranslator.ProcessResult.Processed)
                {
                    UnityEngine.Debug.Log(dllName + " process yet!");
                    return;
                }

                // tranlater.Serialize(args[4]);

                assembly.Write(assemblyOutputPath, new WriterParameters { WriteSymbols = readSymbols });
                //ilfixAassembly.Write(args[2], new WriterParameters { WriteSymbols = true });
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("Unhandled Exception:\r\n" + e);
                return;
            }
            finally
            {
                //清理符号读取器
                //如果不清理，在window下会锁定文件
                if (assembly != null && assembly.MainModule.SymbolReader != null)
                {
                    assembly.MainModule.SymbolReader.Dispose();
                }
                if (ilfixAassembly != null && ilfixAassembly.MainModule.SymbolReader != null)
                {
                    ilfixAassembly.MainModule.SymbolReader.Dispose();
                }
            }
            UnityEngine.Debug.Log(dllName + " inject success");
        }
    }

    #region GenerateConfigure
    public abstract class GenerateConfigure
    {
        //仅仅简单的从文件加载类名而已
        public static GenerateConfigure FromFile(string filename)
        {
            DefaultGenerateConfigure generateConfigure = new DefaultGenerateConfigure();

            using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                int configureNum = reader.ReadInt32();
                for (int i = 0; i < configureNum; i++)
                {
                    string configureName = reader.ReadString();
                    Dictionary<string, int> configure = new Dictionary<string, int>();
                    int cfgItemCount = reader.ReadInt32();
                    for (int j = 0; j < cfgItemCount; j++)
                    {
                        string typeName = reader.ReadString();
                        int flag = reader.ReadInt32();
                        configure[typeName] = flag;
                    }
                    generateConfigure.configures[configureName] = configure;
                }
                generateConfigure.blackListMethodInfo = readMatchInfo(reader);
            }

            return generateConfigure;
        }

        /// <summary> 如果一个方法打了指定的标签，返回其配置的标志位 </summary>
        /// <param name="tag">标签</param>
        /// <param name="method">要查询的方法</param>
        /// <param name="flag">输出参数，用户配置的标志位</param>
        /// <returns></returns>
        public abstract bool TryGetConfigure(string tag, MethodReference method, out int flag);

        /// <summary> 判断一个方法是否是新增方法 </summary>
        /// <param name="method">要查询的方法</param>
        /// <returns></returns>
        public abstract bool IsNewMethod(MethodReference method);

        public abstract bool IsNewClass(TypeReference type);
        //参数类型信息
        internal class ParameterMatchInfo
        {
            public bool IsOut;
            public string ParameterType;
        }

        //方法签名信息
        internal class MethodMatchInfo
        {
            public string Name;
            public string ReturnType;
            public ParameterMatchInfo[] Parameters;
        }

        //判断一个方法是否能够在matchInfo里头能查询到
        internal static bool isMatch(Dictionary<string, MethodMatchInfo[]> matchInfo, MethodReference method)
        {
            MethodMatchInfo[] mmis;
            if (matchInfo.TryGetValue(method.DeclaringType.FullName, out mmis))
            {
                foreach (var mmi in mmis)
                {
                    if (mmi.Name == method.Name && mmi.ReturnType == method.ReturnType.FullName
                        && mmi.Parameters.Length == method.Parameters.Count)
                    {
                        bool paramMatch = true;
                        for (int i = 0; i < mmi.Parameters.Length; i++)
                        {
                            var paramType = method.Parameters[i].ParameterType;
                            if (paramType.IsRequiredModifier)
                            {
                                paramType = (paramType as RequiredModifierType).ElementType;
                            }
                            if (mmi.Parameters[i].IsOut != method.Parameters[i].IsOut
                                || mmi.Parameters[i].ParameterType != paramType.FullName)
                            {
                                paramMatch = false;
                                break;
                            }
                        }
                        if (paramMatch) return true;
                    }
                }
            }
            return false;
        }

        internal static bool isMatchForClass(HashSet<string> matchInfo, TypeReference type)
        {
            if (matchInfo.Contains(type.ToString()))
            {
                return true;
            }
            return false;
        }

        //读取方法信息，主要是方法的签名信息，名字+参数类型+返回值类型
        internal static Dictionary<string, MethodMatchInfo[]> readMatchInfo(BinaryReader reader)
        {
            Dictionary<string, MethodMatchInfo[]> matchInfo = new Dictionary<string, MethodMatchInfo[]>();

            int typeCount = reader.ReadInt32();
            for (int k = 0; k < typeCount; k++)
            {
                string typeName = reader.ReadString();
                int methodCount = reader.ReadInt32();
                MethodMatchInfo[] methodMatchInfos = new MethodMatchInfo[methodCount];
                for (int i = 0; i < methodCount; i++)
                {
                    MethodMatchInfo mmi = new MethodMatchInfo();
                    mmi.Name = reader.ReadString();
                    mmi.ReturnType = reader.ReadString();
                    int parameterCount = reader.ReadInt32();
                    mmi.Parameters = new ParameterMatchInfo[parameterCount];
                    for (int p = 0; p < parameterCount; p++)
                    {
                        mmi.Parameters[p] = new ParameterMatchInfo();
                        mmi.Parameters[p].IsOut = reader.ReadBoolean();
                        mmi.Parameters[p].ParameterType = reader.ReadString();
                    }
                    methodMatchInfos[i] = mmi;
                }
                matchInfo[typeName] = methodMatchInfos;
            }

            return matchInfo;
        }
        internal static HashSet<string> readMatchInfoForClass(BinaryReader reader)
        {
            HashSet<string> setMatchInfoForClass = new HashSet<string>();
            int typeCount = reader.ReadInt32();
            for (int k = 0; k < typeCount; k++)
            {
                string className = reader.ReadString();
                setMatchInfoForClass.Add(className);
            }
            return setMatchInfoForClass;
        }
    }

    //注入配置使用
    public class DefaultGenerateConfigure : GenerateConfigure
    {
        internal Dictionary<string, Dictionary<string, int>> configures
            = new Dictionary<string, Dictionary<string, int>>();

        internal Dictionary<string, MethodMatchInfo[]> blackListMethodInfo = null;

        public override bool TryGetConfigure(string tag, MethodReference method, out int flag)
        {
            Dictionary<string, int> configure;
            flag = 0;
            if (tag == "IFix.IFixAttribute" && blackListMethodInfo != null)
            {
                if (isMatch(blackListMethodInfo, method))
                {
                    return false;
                }
            }
            return (configures.TryGetValue(tag, out configure)
                && configure.TryGetValue(method.DeclaringType.FullName, out flag));
        }

        public override bool IsNewMethod(MethodReference method)
        {
            return false;
        }
        public override bool IsNewClass(TypeReference type)
        {
            return false;
        }
    } 
    #endregion
}

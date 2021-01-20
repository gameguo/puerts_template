using System.Reflection;
using UnityEditor;

public static class UnityEditorUtility
{
    [MenuItem("Utility/Compilation")]
    public static void CompilationEditor()
    {
        Compilation();
    }

    /// <summary> 重新编译脚本 </summary>
    public static void Compilation()
    {
        if (EditorApplication.isCompiling || UnityEngine.Application.isPlaying)
        {
            UnityEngine.Debug.LogWarning("Compiling...");
            return;
        }
        // 重新编译 即clear
#if UNITY_2019_3_OR_NEWER
        UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
#elif UNITY_2017_1_OR_NEWER
        // UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface.DirtyAllScripts();
        var editorAssembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
        var editorCompilationInterfaceType = editorAssembly.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface");
        var dirtyAllScriptsMethod = editorCompilationInterfaceType.GetMethod("DirtyAllScripts", BindingFlags.Static | BindingFlags.Public);
        dirtyAllScriptsMethod.Invoke(editorCompilationInterfaceType, null);
#else
        UnityEngine.Debug.LogWarning("Need -- Unity 2017.1 OR NEWER");
#endif
        UnityEngine.Debug.Log("Compiling Success");
    }
}

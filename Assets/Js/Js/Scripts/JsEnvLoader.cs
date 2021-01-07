using Puerts;
using System.IO;

/// <summary> 自定义Loader </summary>
public class JsEnvLoader : ILoader
{
    private string root = "";
    public JsEnvLoader(string root)
    {
        if (Path.IsPathRooted(root)) this.root = root;
        else this.root = Path.Combine(System.Environment.CurrentDirectory, root);
    }
    public bool FileExists(string filepath)
    {
        if (File.Exists(GetPath(filepath))) return true;
        return UnityEngine.Resources.Load(GetResourcePath(filepath)) != null;
    }
    public string ReadFile(string filepath, out string debugpath)
    {
        debugpath = GetPath(filepath);
        if (File.Exists(debugpath)) return File.ReadAllText(debugpath);
        return UnityEngine.Resources.Load<UnityEngine.TextAsset>(GetResourcePath(filepath)).text;
    }
    private string GetResourcePath(string filepath)
    {
        return filepath.EndsWith(".cjs") ?
            filepath.Substring(0, filepath.Length - 4) :
            filepath;
    }
    private string GetPath(string filepath)
    {
        var result = Path.Combine(root, filepath);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        result = result.Replace("/", "\\");
#endif
        return result;
    }
}

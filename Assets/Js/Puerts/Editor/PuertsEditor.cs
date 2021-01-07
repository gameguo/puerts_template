using Puerts.Editor;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class PuertsEditor
{
    /// <summary> 要生成的路径 </summary>
    public static string DataPath { get { return Application.dataPath + "/Js/Puerts"; } }

    [MenuItem("Hotfix/Puerts/Generate Code", false, 1)]
    public static void GenerateCode()
    {
        var start = DateTime.Now;
        var saveTo = DataPath + "/Gen/";
        Directory.CreateDirectory(saveTo);
        Directory.CreateDirectory(saveTo + "Typing/csharp");
        Generator.GenerateCode(saveTo);
        Debug.Log("finished! use " + (DateTime.Now - start).TotalMilliseconds + " ms");
        AssetDatabase.Refresh();
    }
    [MenuItem("Hotfix/Puerts/Generate index.d.ts", false, 1)]
    public static void GenerateDTS()
    {
        var start = DateTime.Now;
        var saveTo = DataPath + "/Gen/";
        Directory.CreateDirectory(saveTo);
        Directory.CreateDirectory(saveTo + "Typing/csharp");
        Generator.GenerateCode(saveTo, true);
        Debug.Log("finished! use " + (DateTime.Now - start).TotalMilliseconds + " ms");
        AssetDatabase.Refresh();
    }
    [MenuItem("Hotfix/Puerts/Clear Generated Code", false, 2)]
    public static void ClearAll()
    {
        var saveTo = DataPath + "/Gen/";
        if (Directory.Exists(saveTo))
        {
            Directory.Delete(saveTo, true);
            AssetDatabase.DeleteAsset(saveTo.Substring(saveTo.IndexOf("Assets") + "Assets".Length));
            AssetDatabase.Refresh();
        }
    }
}
using Puerts;
using System;
using System.Collections.Generic;
using UnityEngine;

[Configure]
public class PuertsCfg
{
    [CodeOutputDirectory] // Bind代码生成目录
    public static string OutputGenDirectory { get { return Application.dataPath + "/Js/Puerts/Gen/"; } }

    [Binding]
    static IEnumerable<Type> Bindings
    {
        get
        {
            return new List<Type>()
            {
                typeof(Time),
                typeof(Transform),
                typeof(Component),
                typeof(GameObject),
                typeof(UnityEngine.Object),
                typeof(Behaviour),
                typeof(MonoBehaviour),
            };
        }
    }

    [BlittableCopy]
    static IEnumerable<Type> Blittables
    {
        get
        {
            return new List<Type>()
            {
                //打开这个可以优化Vector3的GC，但需要开启unsafe编译
                //typeof(Vector3),
            };
        }
    }

    [Filter] // 过滤器
    static bool FilterMethods(System.Reflection.MemberInfo mb)
    {
        // 排除 MonoBehaviour.runInEditMode, 在 Editor 环境下可用发布后不存在
        if (mb.DeclaringType == typeof(MonoBehaviour) && mb.Name == "runInEditMode")
        {
            return true;
        }
        return false;
    }
}

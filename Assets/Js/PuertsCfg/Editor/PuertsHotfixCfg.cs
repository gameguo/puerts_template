using Puerts;
using System;
using System.Collections.Generic;

[Configure]
public class PuertsHotfixCfg
{
    [HotfixList]
    static IEnumerable<Type> Hotfixs
    {
        get
        {
            return new List<Type>() 
            {
                typeof(PuertsTest.JsHotfixTest),
            };
        }
    }

    // 觉得JsHotfixTest类里的函数可能会需要修复，但是Test类里面的某个方法不可能有问题，可以把这两个函数过滤掉。
    [HotfixFilter]
    static bool Filter(System.Reflection.MethodInfo methodInfo)
    {
        return methodInfo.DeclaringType.FullName == "PuertsTest.JsHotfixTest"
            && (methodInfo.Name == "NoHotfixTest");
    }
}

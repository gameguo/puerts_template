using Puerts;
using System;
using System.Collections.Generic;

[Configure]
public class PuertsHotfixConfig
{
    [HotfixList]
    static IEnumerable<Type> Hotfixs
    {
        get
        {
            return new List<Type>() 
            {
                typeof(JsHotfixTest),
            };
        }
    }

    // 觉得Test类里的函数可能会需要修复，但是Test类里面的Div和Mult不可能有问题，可以把这两个函数过滤掉。
    [HotfixFilter]
    static bool Filter(System.Reflection.MethodInfo methodInfo)
    {
        return methodInfo.DeclaringType.FullName == "Test"
            && (methodInfo.Name == "Div" || methodInfo.Name == "Mult");
    }
}

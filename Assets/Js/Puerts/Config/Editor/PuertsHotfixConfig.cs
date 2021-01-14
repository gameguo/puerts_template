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
}

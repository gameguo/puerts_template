using System;

namespace Puerts
{
    // Hotfix List
    [AttributeUsage(AttributeTargets.Property)]
    public class HotfixListAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public class HotfixFilterAttribute : Attribute { }
}

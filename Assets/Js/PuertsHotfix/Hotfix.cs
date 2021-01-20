namespace Puerts
{
    public class Hotfix
    {


        public static bool HasPatch(string className, string methodName, string methodParams)
        {
#if UNITY_EDITOR
            // UnityEngine.Debug.Log(string.Format("HasPatch: {0}:{1}({2})", className, methodName, methodParams));
#endif

            // TODO 此处函数存在判断
            return false;
        }
        public static object CallPatch(string className, string methodName, string methodParams, params object[] args)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(string.Format("Patch: {0}:{1}({2})", className, methodName, methodParams));
#endif
            // TODO 此处写函数调用, 并传入参数.
            // return Lua.DoFile(luaFile).Call(luaFunc, args);
            return null;
        }
    }
}

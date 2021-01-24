namespace Puerts
{
    public class Hotfix
    {
        public static bool HasPatch(string methodName)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(string.Format("HasPatch: {0}", methodName));
#endif
            // TODO 此处函数存在判断
            return false;
        }
        public static object CallPatch(string methodName, params object[] args)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(string.Format("Patch: {0}", methodName));
#endif
            // TODO 此处写函数调用, 并传入参数.
            // return Lua.DoFile(luaFile).Call(luaFunc, args);
            return null;
        }
    }
}

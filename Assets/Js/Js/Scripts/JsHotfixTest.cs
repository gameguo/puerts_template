using UnityEngine;

namespace PuertsTest
{
    public class JsHotfixTest : MonoBehaviour
    {
        private void Start()
        {
            HotfixTest();
            NoHotfixTest();
        }

        private void HotfixTest()
        {
            Debug.Log("C#的方法 :: HotfixTest");
        }
        private void NoHotfixTest()
        {
            Debug.Log("C#的方法 :: NoHotfixTest");
        }
    } 
}

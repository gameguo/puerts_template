using UnityEngine;

namespace PuertsTest
{
    public class JsHotfixTest : MonoBehaviour
    {
        private void Start()
        {
            HotfixTest();
            HotfixTest(2);
            NoHotfixTest();
        }

        private void HotfixTest()
        {
            Debug.Log("C#的方法 :: HotfixTest");
        }
        private void HotfixTest(int hotfixTest2)
        {
            Debug.Log("C#的方法 :: HotfixTest2 - " + hotfixTest2);
        }
        private void NoHotfixTest()
        {
            Debug.Log("C#的方法 :: NoHotfixTest");
        }

    } 
}

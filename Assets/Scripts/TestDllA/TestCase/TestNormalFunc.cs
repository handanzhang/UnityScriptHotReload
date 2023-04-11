#define ENABLE_OPEN_HOT_RELOAD

using System;
using UnityEngine;

namespace NS_Test.TestCase
{
    public class TestNormalFunc
    {
        public static void Test(ref int val)
        {
            val = 1;
            Func<int, bool> f = x =>
            {
                Debug.Log($"{str}...{x}");
                return x > 101;
            };
            s_lambda = f;
        }
    
        public static string str { get; set; } = "default str";

        public static Func<int, bool> s_lambda;
        
        public static void Test2()
        {
#if ENABLE_OPEN_HOT_RELOAD
            Debug.Log("load new function. succeed. jn game");
            XXX();
#else
            if (s_lambda != null)
            {
                s_lambda.Invoke(1);
            }   
#endif
        }

#if ENABLE_OPEN_HOT_RELOAD

        public static void XXX()
        {
            if (s_lambda != null)
            {
                s_lambda.Invoke(1000);
            } 
            Debug.Log($"{str} .. do now");
        }
#endif

    }
}
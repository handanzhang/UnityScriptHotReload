#define ENABLE_OPEN_HOT_RELOAD

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace NS_Test.TestCase
{

    public class NoCallClass
    {
        private static Dictionary<int, List<int>> s_SceneId2RightUpCornerXZDict = new Dictionary<int, List<int>>();

        public static void XXX()
        {
           
        }
    }
    
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
            
            
            var asm = Assembly.LoadFrom(@"E:\Project-J1\client\Temp\ScriptHotReload\AreaEditor_patch_0.dll");
            var type = asm.GetType("ToolEditor.AreaEditor.AreaVoxelizationHelper");
            var method = type.GetMethod("GenerateTransmitTriggerData", BindingFlags.Public | BindingFlags.Static);
            var handle = method.MethodHandle.GetFunctionPointer();
            
        }
    
        public static string str { get; set; } = "default str";
        
        public string instanceStr { get; set; }

        public static Func<int, bool> s_lambda;

        
        public static void Test2()
        {
#if ENABLE_OPEN_HOT_RELOAD

#else
            if (s_lambda != null)
            {
                s_lambda.Invoke(1);
            }
#endif
        }

#if ENABLE_OPEN_HOT_RELOAD

        public void XXX()
        {
            if (s_lambda != null)
            {
                s_lambda.Invoke(1000);
            } 
            Debug.Log($"{str} .. {instanceStr}");
        }

        public static void XY(TestNormalFunc @this)
        {
            
        }
        
#endif
    }
}
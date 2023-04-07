using System;
using System.Reflection;
using UnityEngine;

namespace NS_Test
{
    public class TestFurther
    {
        public static void Print()
        {
            var x = new TestDll_2();
            Debug.Log(x.Mul_2(5, 2));

            var currMethod = MethodBase.GetCurrentMethod();
            var assPath = currMethod.DeclaringType.Assembly.Location.Substring(Environment.CurrentDirectory.Length + 1);

            Debug.Log($"h location `<color=yellow>{currMethod.Name}</color>` of current dll: <color=yellow>{assPath.Replace('\\', '/')}</color>");
        }
    }
}
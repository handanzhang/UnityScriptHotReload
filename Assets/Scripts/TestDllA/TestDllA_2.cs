using System;
using System.Reflection;
using UnityEngine;

namespace NS_Test
{
    public class TestDll_2
    {
        public int Mul_2(int x, int y)
        {
#if APPLY_PATCH
            Debug.Log("mul_2 hot reload!!!");
#endif

            var b = new SubB();
            PrintMethodLocation_2(b.GetType().GetMethod("Print", BindingFlags.Instance | BindingFlags.Public));
            return x * y + 2;
        }

        private void PrintMethodLocation_2(MethodBase method)
        {
            var currMethod = MethodBase.GetCurrentMethod();
            var assPath = method.DeclaringType.Assembly.Location.Substring(Environment.CurrentDirectory.Length + 1);
            Debug.Log($"location `<color=yellow>{method.Name}</color>` of current dll: <color=yellow>{assPath.Replace('\\', '/')}</color>");
        }
    }
}
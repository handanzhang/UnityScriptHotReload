using UnityEngine;

namespace NS_Test
{
    public class BaseA
    {
        public int a = 200;

        public BaseA()
        {
            Debug.LogWarning($"BaseA a: {a}, c: 1");
        }

        public virtual void Print()
        {
            Debug.Log("z virtual method");
        }

        public void FF()
        {
            Debug.Log("Print A");
        }
    }
}
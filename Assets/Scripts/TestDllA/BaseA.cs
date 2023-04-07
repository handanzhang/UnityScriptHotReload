using UnityEngine;

namespace NS_Test
{
    public class BaseA
    {
        public int a = 200;

        public BaseA()
        {
            Debug.LogWarning($"BaseA a: {a}, c: ");
        }

        public virtual void Print()
        {
            Debug.Log("z virtual method");
        }
    }
}
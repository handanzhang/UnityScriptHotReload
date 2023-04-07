using System;
using UnityEngine;

namespace NS_Test
{
    public class SubB : BaseA
    {
        public SubB() : this(-100)
        {
            Debug.LogWarning($"result a: {a}");
        }


        public SubB(int aa)
        {
            a = aa;
            Debug.LogWarning($"inner a: {a}");
        }

        public void FFF()
        {
            a = 5;
            Debug.Log(a);
        }
        
        public override void Print()
        {
            Debug.Log($"hot reload virtual method B {a}");
        }
    }
}
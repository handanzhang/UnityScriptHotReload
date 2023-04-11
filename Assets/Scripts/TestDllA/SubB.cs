using System;
using UnityEngine;

namespace NS_Test
{
    public class SubB : BaseA
    {
        public SubB() : this(-100)
        {
            Debug.LogWarning($"result a: {a} XXX");
        }


        public SubB(int aa)
        {
            a = aa;
            Debug.LogWarning($"inner a: {a}");
        }

        public new void FF()
        {
            Debug.Log("Sub B");
        }
        
        public override void Print()
        {
            base.Print();
            Debug.Log($"virtual method B {a}");
        }
    }
}
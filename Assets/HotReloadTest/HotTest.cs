using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEditor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using NUnit.Framework;


namespace HOT_RELOAD_TEST
{
    public class HotTest
    {
        private static int _ii = 100;
        
        [MenuItem("haitong/test")]
        public static void Test()
        {
            Debug.Log("JN1_TEST-4554555");

            Debug.Log(_ii);

        }

    }
}



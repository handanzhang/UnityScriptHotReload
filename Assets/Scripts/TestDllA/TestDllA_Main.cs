// #define APPLY_PATCH

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NS_Test
{
#if APPLY_PATCH
    public class NewTestClass
    {
        public static int val;
        public int x;
        static NewTestClass()
        {
            // 请注意，新增类的静态构造函数尽量不要影响其它类数据，且此函数每次被reload都会执行
            val = 5;
            Debug.Log("NewTestClass static constructor");
        }
    
        public int Add(int y)
        {
            return x + y + val;
        }
    }
#endif
    public class TestCls
    {
        public class InnerTest
        {
            public int innerX;

            public void FuncInnerA(int val)
            {
#if !APPLY_PATCH
                Debug.Log($"<color=yellow>FuncInnerA:</color> {innerX + val}");
#else
                Debug.Log($"<color=yellow>FuncInnerA: patched</color> {innerX + val + 1}");
#endif
            }

            public class Inner2Cls<T>
            {
            }
        }

        public static int s_val = 123;
        public static string str { get; set; } = "default str";
        public static string str2 => (s_val + 2).ToString();

        private readonly GameObject _go;
        private readonly InnerTest _innerTest = new InnerTest();
        private static TestClsG<TestCls>.TestClsGInner<TestDll_2> _genericFiledTest;

        public Action<int> act = x =>
        {
            x += 2;
            Debug.Log(x * 3);
        };

        public Action<int> act2 = x =>
        {
            x += 2;
            Debug.Log(x * 3);
            Debug.Log(str2);
        };

        public TestCls(GameObject go)
        {
            Debug.LogWarning($"{go.GetComponent<MonoTestA>() != null}, {typeof(MonoTestA).Module.FullyQualifiedName}");


            _go = go;
        }

        public void Init()
        {
            _innerTest.innerX = 10;
        }

#if !APPLY_PATCH
        static TestCls()
        {
            Debug.Log("static constructor");
        }

        public void FuncA(out int val)
        {

            
            val = 2;
            
            var bb = new SubB();
            bb.Print();

            Func<int, bool> f = x =>
            {
                Debug.Log($"{x + 1}-{str}...");
                return x > 101;
            };
            Debug.Log($"x is OK:{f(val + 2)}");
            TestB();
            TestC();
            Test4.val = 10;
            Debug.Log($"Test4.val={Test4.val} from Test()");
            str = "be happy";
            Debug.Log(str);

            _innerTest.FuncInnerA(5);

            var test2 = new TestDll_2();
            var z = test2.Mul_2(1, 5);
            Debug.Log($"test2={z + 2 + 1 + 10000}");

            var valB = TestDllB_Main.Calc(1, 5);
            Debug.Log($"valB from Ref dll = {valB}");

            PrintMethodLocation(MethodBase.GetCurrentMethod());
        }

        public List<Transform> GetAllDeactiveOvbjs()
        {
            return new List<Transform>();
        }

        public InnerTest.Inner2Cls<Dictionary<string, Transform>> ReturnNestGenericType()
        {
            return null;
        }
#else
        static TestCls()
        {
            Debug.Assert(false, "static constructor of patched type can not be invoke");
            Debug.Log("static constructor patched");
        }


        private static SubB _testB;

        private static Vector3 _testVector;
        public void FuncA(out int val)
        {
            val = 2;

            NewClass.Print();
            
            var newC = new NewClass();
            Debug.Log(newC.ReturnStr());

            new SubB().Print();

            TestB();

            val = 461;
            Func<int, bool> f = (int x) => { Debug.Log($"{x + 10}-{str}..."); return x > 101; };
            Debug.Log($"x is OK:{f(val + 2)}");
            TestB();
            TestC();
            Debug.Log($"Test4.val={Test4.val} from Test()");
            Debug.Log(str);
            FuncNew();
            
            _innerTest.FuncInnerA(5);
            
            int valB = TestDllB_Main.Calc(1, 6);
            Debug.Log($"valB from Ref dll = {valB}");
            
            var newCls = new NewTestClass();
            newCls.x = 1;
            Debug.Log($"NewTestClass.Add:{newCls.Add(3)}");
            
            var test2 = new TestDll_2();
            int z = test2.Mul_2(5, 6);
            Debug.Log($"test2={z+2+1+10000}");
            
            PrintMethodLocation(MethodBase.GetCurrentMethod());
        }
        public void FuncNew()
        {
            //Func<int, bool> f2 = (int x) => { Debug.Log($"{x + 1} $$$ {str}..."); return x > 100; };
            //Func<int, bool> f3 = (int x) => { Debug.Log($"{x + 1}@@@"); return x > 200; };
            //Debug.Log("this is Test3__" + f2(456));

            PrintMethodLocation(MethodBase.GetCurrentMethod());
        }

        // add new virtual method to exists type is not allowd
        //public virtual void FuncVirtualNew()
        //{
        //}

        public List<Transform> GetAllDeactiveObjs()
        {
            return new List<Transform>(10);
        }

        public InnerTest.Inner2Cls<Dictionary<string, Transform>> ReturnNestGenericType()
        {
            return new InnerTest.Inner2Cls<Dictionary<string, Transform>>();
        }
#endif
        private static Dictionary<string, SubB> _testDict;

        public void TestB()
        {
            
            Debug.LogWarning($"{_go.GetComponent<Transform>()}");
            Debug.LogWarning(new List<Collider>());

            
            Debug.LogWarning(TestClsG<TestCls>.TestClsGInner<TestDll_2>.innerField_i);
            
            _genericFiledTest = new TestClsG<TestCls>.TestClsGInner<TestDll_2>();
            _genericFiledTest.FF();


            if (_testDict == null)
            {
                _testDict = new Dictionary<string, SubB>();
                _testDict.Add("a", new SubB());
            }

            if (!_testDict.ContainsKey("b")) _testDict.Add("b", new SubB(300));
            Debug.Log(_testDict["b"].a);
            Debug.Log(_testDict["a"].a);

            foreach (var pair in _testDict) Debug.LogWarning($"{pair.Key}, {pair.Value.a}");

            var t = typeof(int);
            Debug.LogWarning(t.Name + "");

            t = typeof(TestCls);
            Debug.Log(t.Module.FullyQualifiedName);

            var tG = typeof(TestClsG<TestCls>.TestClsGInner<string>);
            Debug.Log(tG.Module.FullyQualifiedName);

            if (_genericFiledTest == null)
            {
                _genericFiledTest = new TestClsG<TestCls>.TestClsGInner<TestDll_2>();
                TestClsG<TestCls>.TestClsGInner<TestDll_2>.innerField_i = 270;
                Debug.LogWarning($" is yy?? {TestClsG<TestCls>.TestClsGInner<TestDll_2>.innerField_i}");
                _genericFiledTest.innerField_V = new TestDll_2();
            }

            Debug.Log($"generic {TestClsG<TestCls>.TestClsGInner<TestDll_2>.innerField_i}");

            TestClsG<TestCls>.TestClsGInner<TestDll_2>.innerField_i = 5;

            var val0 = _genericFiledTest.ShowInner(2);

            var val1 = _genericFiledTest.ShowGInner(this, null, 321.0);
            var val2 = _genericFiledTest.FuncG(this, "test words", null);

            var tmpGenericObj = new TestClsG<TestCls>.TestClsGInner<TestDll_2>();
            Func<TestCls, string, TestDll_2, TestDll_2> funcG = tmpGenericObj.FuncG;
            var val3 = funcG(this, "test words 2", null);

            var comp = _go.GetComponent<MonoTestA>();
            Debug.Log("components text" + comp.text);

            var bb = new SubB();
            var x = new TestDll_2();
            x.Mul_2(5, 2);
            PrintMethodLocation(bb.GetType().GetMethod("Print", BindingFlags.Instance | BindingFlags.Public));
        }


        public void TestC()
        {
            PrintMethodLocation(MethodBase.GetCurrentMethod());
            TestB();
        }

        public T TestG<T>(T t) where T : new()
        {
            Debug.Log($"t.type is:{t.GetType()}");
            return new T();
        }

        private void PrintMethodLocation(MethodBase method)
        {
            Debug.LogWarning("hot reload new uuxxyy!!!!");
            var currMethod = MethodBase.GetCurrentMethod();
            var assPath = method.DeclaringType.Assembly.Location.Substring(Environment.CurrentDirectory.Length + 1);
            Debug.Log($"location `<color=yellow>{method.Name}</color>` of current dll: <color=yellow>{assPath.Replace('\\', '/')}</color>");
        }
    }

    public class Test4
    {
        public static int val = 2;
    }

    public class TestClsG<T>
    {
        public string str;

        public T ShowGA<U>(T arg0, U arg1)
        {
            Debug.Log($"ShowA, T is:{typeof(T).GetType()}, U is:{typeof(U).GetType()}");
            return arg0;
        }

        public U ShowGB<U>(T arg0, U arg1)
        {
            Debug.Log($"ShowB, T is:{typeof(T).GetType()}, U is:{typeof(U).GetType()}");
            return arg1;
        }

        public class TestClsGInner<V>
        {
            public static int innerField_i;
            public V innerField_V;

            public int ShowInner(int x)
            {
                var val1 = ShowGInner<long>(default, default, 2345);


                // var val2 = FuncG(default(T), "abc", default(V));
#if !APPLY_PATCH
                return x + 1;
#else
                return x + 2;
#endif
            }

            public V ShowGInner<UK>(T arg0, V arg1, UK arg2)
            {
                Debug.LogWarning($"ShowInner, T is:{typeof(T).GetType()}, U is:{typeof(UK).GetType()}");
                return arg1;
            }

            public void FF()
            {
                Debug.LogWarning(typeof(T).GetType());
            }

            public V FuncG(T arg0, string arg1, V arg2)
            {
                return arg2;
            }
        }
    }
}
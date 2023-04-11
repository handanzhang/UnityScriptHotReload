using NS_Test;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public Button btnTest;

    // Start is called before the first frame update
    private void Start()
    {
        btnTest.onClick.AddListener(OnBtnTest);
        TestCls.s_val = 456;
        Test4.val = 1234;
    }

    // Update is called once per frame
    private void Update()
    {
        transform.position += new Vector3(0, 0, 0.1f);
    }

    private void OnBtnTest()
    {
        int a = 1;
        NS_Test.TestCase.TestNormalFunc.Test(ref a);
        NS_Test.TestCase.TestNormalFunc.Test2();

        // TestFurther.Print();
        // var test = new TestCls(gameObject);
        //
        // test.FuncA(out var val);
        // Debug.Log($"OnBtnTest:val={val}");
        //
        // Debug.Log($"Test3.s_val={TestCls.s_val}");
    }
}
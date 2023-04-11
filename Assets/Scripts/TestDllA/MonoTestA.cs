using UnityEngine;
using UnityEngine.UI;

public class MonoTestA : MonoBehaviour
{
    public Text text;

    // Update is called once per frame
    private void Update()
    {
    }

    public void ShowText()
    {
        Debug.Log($"1111 MonoTestA.ShowText: text is:\"{text.text}\"");
    }
}

namespace NS_Test
{
    public class NewClass
    {
        public static void Print()
        {
            Debug.Log("Hot Reload new Func12");
        }

        public string ReturnStr()
        {
            return "XXXX";
        }

        public SubB GetB()
        {
            return new SubB();
        }
    }
}
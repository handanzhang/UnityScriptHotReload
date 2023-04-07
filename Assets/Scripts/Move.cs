using UnityEngine;

public class Move : MonoBehaviour
{
    private readonly float speed = 3f;

    // Start is called before the first frame update
    private void Start()
    {
        Debug.LogWarning("move mono start");
    }

    // Update is called once per frame
    private void Update()
    {
        var pos = gameObject.GetComponent<Transform>().position;
        if (Input.GetKeyDown(KeyCode.A))
            pos.x -= speed * Time.deltaTime;
        else if (Input.GetKeyDown(KeyCode.D))
            pos.x += speed * Time.deltaTime;
        else if (Input.GetKeyDown(KeyCode.W))
            pos.z += speed * Time.deltaTime;
        else if (Input.GetKeyDown(KeyCode.S)) pos.x -= speed * Time.deltaTime;

        gameObject.GetComponent<Transform>().position = pos;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"collision eneter {collision.gameObject.name}");
    }

    private void OnCollisionExit(Collision other)
    {
        Debug.Log($"collision exit {other.gameObject.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"trigger enter {other.gameObject.name}");
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"trigger exit {other.gameObject.name}");
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    private Camera m_Camera;

    // Start is called before the first frame update
    void Start()
    {
        m_Camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 dir = new Vector3(0f, 0f, 0f);

        if (Input.GetKey(KeyCode.W))
            dir += new Vector3(0f, 1f, 0f);
        if (Input.GetKey(KeyCode.S))
            dir += new Vector3(0f, -1f, 0f);
        if (Input.GetKey(KeyCode.D))
            dir += new Vector3(1f, 0f, 0f);
        if (Input.GetKey(KeyCode.A))
            dir += new Vector3(-1f, 0f, 0f);


        Camera.main.orthographicSize -= Input.mouseScrollDelta.y;
        Camera.main.orthographicSize = Mathf.Max(Camera.main.orthographicSize, 1f);

        dir.Normalize();


        m_Camera.transform.Translate(dir * (Time.deltaTime * Camera.main.orthographicSize * 2f));
    }
}
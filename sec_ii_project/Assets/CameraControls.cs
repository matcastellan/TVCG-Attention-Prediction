using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControls : MonoBehaviour
{

    public float mouseSensitivity = 100f;
    private float xRotation = 0f;
    private float yRotation = 0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, Camera.main.transform.position.z);
        if (Input.GetKey(KeyCode.E))
        {
            position += Camera.main.transform.up * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            position -= Camera.main.transform.up * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.W))
        {
            position += Camera.main.transform.forward * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            position -= Camera.main.transform.forward * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.A))
        {
            position -= Camera.main.transform.right * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            position += Camera.main.transform.right * Time.deltaTime;
        }

        Camera.main.transform.position = position;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        yRotation += mouseX;

        Camera.main.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}

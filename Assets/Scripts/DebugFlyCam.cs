using UnityEngine;

public class DebugFlyCamera : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;           
    public float fastMoveMultiplier = 3f;   

    [Header("Look Settings")]
    public float mouseSensitivity = 2f; 

    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        Cursor.visible = false;

        Vector3 angles = transform.eulerAngles;
        xRotation = angles.x;
        yRotation = angles.y;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);

        float currentSpeed = moveSpeed;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= fastMoveMultiplier;
        }

        float x = Input.GetAxis("Horizontal"); 
        float z = Input.GetAxis("Vertical");   
        float y = 0f;

        if (Input.GetKey(KeyCode.E)) y = 1f;
        if (Input.GetKey(KeyCode.Q)) y = -1f;

        Vector3 moveDirection = transform.right * x + transform.up * y + transform.forward * z;

        transform.position += moveDirection * currentSpeed * Time.deltaTime;
    }
}
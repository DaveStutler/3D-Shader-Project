using UnityEngine;
using UnityEngine.InputSystem; 

public class PlayerShooter : MonoBehaviour
{
    public GameObject bulletPrefab; 
    public float shootForce = 30f;  
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }
    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * shootForce;
        }
    }
}
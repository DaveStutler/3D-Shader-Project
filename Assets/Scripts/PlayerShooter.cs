using UnityEngine;
using UnityEngine.InputSystem; 

public class PlayerShooter : MonoBehaviour
{
    public GameObject bulletPrefab;
    public GameObject grenadePrefab;
    public float shootForce = 100f;  
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot(bulletPrefab);
        }
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Shoot(grenadePrefab);
        }
    }
    void Shoot(GameObject prefab)
    {
        GameObject instance = Instantiate(prefab, transform.position, transform.rotation);
    }
}
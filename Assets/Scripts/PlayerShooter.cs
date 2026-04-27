using UnityEngine;
using UnityEngine.InputSystem;

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject bulletPrefab;
    public GameObject grenadePrefab;      
    public GameObject smokeGrenadePrefab; 

    [Header("Shooting Settings")]
    public Transform firePoint;
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

        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            Shoot(smokeGrenadePrefab);
        }
    }

    void Shoot(GameObject prefab)
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Quaternion spawnRot = firePoint != null ? firePoint.rotation : transform.rotation;
        Vector3 forwardDir = firePoint != null ? firePoint.forward : transform.forward;

        GameObject instance = Instantiate(prefab, spawnPos, spawnRot);

        SmokeGrenade smokeScript = instance.GetComponent<SmokeGrenade>();

        if (smokeScript != null)
        {
            smokeScript.Throw(forwardDir);
        }
        else
        {
            Rigidbody rb = instance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(forwardDir * shootForce, ForceMode.VelocityChange);
            }
        }
    }
}
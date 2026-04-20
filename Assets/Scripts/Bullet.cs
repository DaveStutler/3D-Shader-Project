using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f; 
    private Rigidbody rb;
    private SmokeUpdater updater;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * speed;
        updater = Object.FindFirstObjectByType<SmokeUpdater>();
    }
    void Update()
    {
        if (updater != null)
        {
            updater.AddVelocityAtWorldPos(transform.position, transform.forward * 500.0f, 3.0f);
        }
    }
}
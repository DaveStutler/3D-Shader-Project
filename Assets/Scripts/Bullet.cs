using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 100f;
    public float impactForce = 1000.0f;
    public float impactRadius = 5.0f;
    private Rigidbody rb;
    private SmokeUpdater updater;
    private Vector3 lastPosition;
    void Start()
    {
        lastPosition = transform.position;
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * speed;
        updater = Object.FindFirstObjectByType<SmokeUpdater>();
    }
    void Update()
    {
        if (updater != null)
        {
            Vector3 midPoint = (transform.position + lastPosition) * 0.5f;
            updater.AddVelocityAtWorldPos(lastPosition, transform.forward * impactForce, impactRadius);
            updater.AddVelocityAtWorldPos(midPoint, transform.forward * impactForce, impactRadius);
            updater.AddVelocityAtWorldPos(transform.position, transform.forward * impactForce, impactRadius);

            lastPosition = transform.position;
        }
    }
}
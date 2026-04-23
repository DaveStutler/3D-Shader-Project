using UnityEngine;

public class Grenade : MonoBehaviour
{
    public float speed = 15f;
    public float explosionForce = 5000f; // 爆炸力度
    public float explosionRadius = 8f;   // 爆炸影响范围

    private Rigidbody rb;
    private SmokeUpdater updater;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // 确保不使用重力
        rb.linearVelocity = transform.forward * speed;
        updater = Object.FindFirstObjectByType<SmokeUpdater>();
    }

    // 碰到物体就爆炸
    private void OnCollisionEnter(Collision collision)
    {
        if (updater != null)
        {
            // 调用我们刚才写的径向爆炸逻辑
            updater.AddExplosionForceAtWorldPos(transform.position, explosionForce, explosionRadius);
        }

        // 销毁手雷本身
        Destroy(gameObject);
    }
}
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SmokeGrenade : MonoBehaviour
{
    public float throwForce = 15f;
    public float spinForce = 10f;

    public float maxFuseTime = 5.0f;
    public float restVelocityThreshold = 0.05f;

    public GameObject smokeGridPrefab;
    public Vector3 spawnOffset = new Vector3(0f, 0.2f, 0f);

    private Rigidbody rb;
    private bool isThrown = false;
    private bool hasDetonated = false;
    private float timer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void Update()
    {
        if (!isThrown || hasDetonated) return;

        timer += Time.deltaTime;

        if (timer >= maxFuseTime)
        {
            Detonate();
            return;
        }
    }

    public void Throw(Vector3 direction)
    {
        isThrown = true;
        rb.isKinematic = false; // 开启物理

        rb.AddForce(direction.normalized * throwForce, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * spinForce, ForceMode.VelocityChange);
    }

    private void Detonate()
    {
        if (hasDetonated) return;
        hasDetonated = true;

        if (smokeGridPrefab != null)
        {
            Instantiate(smokeGridPrefab, transform.position + spawnOffset, Quaternion.identity);
        }
        else
        {
            Debug.LogError("SmokeGrid Prefab 丢失！请在 Inspector 中赋值。");
        }

        if (TryGetComponent<MeshRenderer>(out var renderer)) renderer.enabled = false;
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;

        Destroy(gameObject, 2f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Detonate();
    }
}
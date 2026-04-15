using UnityEngine;

public class SmokeGrid : MonoBehaviour
{
    // mainvoxel : (density of smoke, velocity_X, velocity_Y, velocity_Z) 
    public Vector3 spaceScale = new Vector3(10f, 10f, 10f);

    public float densityMulptiplier = 10.0f;

    // temp
    [SerializeField]
    private SmokeUpdater _smokeupdater = null;

    [Header("Debug Gizmos")]
    public bool showGizmos = true;
    public bool drawOnlyBounds = true;


    [Header("Reference")]
    [SerializeField] private VoxelGrid mainVoxelGrid;
    [SerializeField] private CollisionMasker collisionMasker;

    [SerializeField] private SmokeRenderer smokeRenderer;
    [SerializeField] private Material material;

    [SerializeField] private DebugVoxelGrid debugVoxelGrid;

    void Awake()
    {
        mainVoxelGrid.spaceScale = spaceScale;

        mainVoxelGrid.initGrid();
        smokeRenderer.Init(transform.position, transform.rotation, spaceScale);

        collisionMasker = GetComponent<CollisionMasker>();

        // just for example
        GenerateSample();
        smokeRenderer.SetMaterial(material);
        smokeRenderer.SetMainVoxelGrid(mainVoxelGrid.mainGrid);
    }

    private void Start()
    {
        collisionMasker.initialize(mainVoxelGrid);

        _smokeupdater.CollisionVoxelGrid = collisionMasker.GetCollisionVoxel();
        _smokeupdater.initializePixels();

        debugVoxelGrid.initialize();
    }

    void GenerateSample(float noiseScale = 0.2f)
    {
        // main voxel
        Color[] colors = new Color[mainVoxelGrid.resolution.x * mainVoxelGrid.resolution.y * mainVoxelGrid.resolution.z];

        for (int z = 0; z < mainVoxelGrid.resolution.z; z++)
        {
            for (int y = 0; y < mainVoxelGrid.resolution.y; y++)
            {
                for (int x = 0; x < mainVoxelGrid.resolution.x; x++)
                {
                    float px = x * noiseScale;
                    float py = y * noiseScale;
                    float pz = z * noiseScale;

                    float noiseValue = Mathf.PerlinNoise(px, py) * Mathf.PerlinNoise(py, pz);

                    Vector3 pos = mainVoxelGrid.GetGridPosition(x, y, z);
                    float distToCenter = Vector3.Distance(pos, transform.position);
                    float mask = Mathf.Clamp01((10.0f - distToCenter * 2.0f) / 10.0f);

                    float finalDensity = noiseValue * mask * densityMulptiplier;

                    // give random speed to the grid
                    Vector3 velocity = Random.insideUnitSphere;

                    int idx = x + y * mainVoxelGrid.resolution.x + z * mainVoxelGrid.resolution.y * mainVoxelGrid.resolution.x;
                    colors[idx] = new Color(finalDensity, velocity.x, velocity.y, velocity.z);
                }
            }
        }

        mainVoxelGrid.mainGrid.SetPixels(colors);
        mainVoxelGrid.mainGrid.Apply();
    }


    void Update()
    {

    }

    public VoxelGrid GetMainGrid()
    {
        return mainVoxelGrid;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        float resX = Mathf.Max(mainVoxelGrid.resolution.x, 1);
        float resY = Mathf.Max(mainVoxelGrid.resolution.y, 1);
        float resZ = Mathf.Max(mainVoxelGrid.resolution.z, 1);

        if (drawOnlyBounds)
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, spaceScale);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            return;
        }

        if (mainVoxelGrid.mainGrid == null) return;

        Color[] voxelData;
        try
        {
            voxelData = mainVoxelGrid.mainGrid.GetPixels();
        }
        catch
        {
            return;
        }

        int startZ = 0;
        int endZ = (int)resZ;


        float voxelHeight = spaceScale.y / resY;
        float maxVisualLength = voxelHeight * 0.5f;

        float maxExpectedDensity = densityMulptiplier;
        float velocityVisualScale = 1.0f;

        for (int z = startZ; z < endZ; z++)
        {
            for (int y = 0; y < resY; y++)
            {
                for (int x = 0; x < resX; x++)
                {
                    int idx = x + y * (int)resX + z * (int)resY * (int)resX;
                    if (idx >= voxelData.Length) continue;

                    Color data = voxelData[idx];

                    float density = data.r;
                    Vector3 localVelocity = new Vector3(data.g, data.b, data.a);
                    float speed = localVelocity.magnitude;

                    if (density < 0.01f) continue;

                    Vector3 pos = mainVoxelGrid.GetGridPosition(x, y, z);

                    float normalizedDensity = Mathf.Clamp01(density / maxExpectedDensity);
                    Gizmos.color = Color.Lerp(Color.blue, Color.red, normalizedDensity);

                    if (speed > 0.001f)
                    {
                        float targetLength = speed * velocityVisualScale;
                        float finalLength = Mathf.Min(targetLength, maxVisualLength);

                        Vector3 rayDirLocal = localVelocity.normalized * finalLength;

                        Vector3 rayDirWorld = transform.TransformDirection(rayDirLocal);

                        Gizmos.DrawRay(pos, rayDirWorld);
                    }
                    else
                    {
                        Gizmos.DrawRay(pos, transform.up * 0.01f);
                    }
                }
            }
        }
    }
}

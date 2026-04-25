using UnityEngine;

public class SmokeUpdater : MonoBehaviour
{
    [Header("Density Update Settings")]
    [SerializeField] private float densityThreshold = 0.0001f;
    [SerializeField] private float minDensityDelta = -0.3f;
    [SerializeField] private float maxDensityDelta = 0.0f;
    [SerializeField] private float minDensity = 0.0f;
    [SerializeField] private float maxDensity = 5.0f;
    [SerializeField] private float smokeDuration = 5.0f;

    [Header("Growth Settings")]
    [SerializeField] private float growStageDuration = 1.0f;
    [SerializeField] private float maxCloudRadius = 5.0f;
    [Tooltip("The fraction of the total growth time a single voxel takes to reach full density. 0.1 = 10% of total time.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float localGrowthRatio = 0.5f;

    private float timeCounter;

    private Color[] initialPixels;
    private Color[] currentPixels;
    private float[] voxelDistances;
    private Color[] lastFramePixels;

    private bool[] _collisionMask;

    public VoxelGrid MainVoxelGrid;
    public VoxelGrid CollisionVoxelGrid;
    public float advectionStrength = 0.8f;
    public float velocityDamping = 0.98f;
    public float regrowthRate = 0.5f;


    private int _originVoxelX, _originVoxelY, _originVoxelZ;
    private float dynamicMaxCost = 1.0f;

    public void InitializePixels()
    {
        Debug.Log("[Smoke Updater] Entered InitalizePixels: " + Time.time);

        // build collision mask
        if (CollisionVoxelGrid != null)
        {
            Color[] colors = CollisionVoxelGrid.mainGrid.GetPixels();
            _collisionMask = new bool[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                if (colors[i].r > 0.1f)
                {
                    _collisionMask[i] = true;
                }
                else
                {
                    _collisionMask[i] = false;
                }
            }
        }


        if (MainVoxelGrid == null || MainVoxelGrid.mainGrid == null) return;

        Texture3D grid = MainVoxelGrid.mainGrid;

        initialPixels = grid.GetPixels();
        currentPixels = new Color[initialPixels.Length];
        voxelDistances = new float[initialPixels.Length];

        Vector3 centerPos = transform.position;
        Vector3Int res = MainVoxelGrid.resolution;

        _originVoxelX = Mathf.Clamp(Mathf.RoundToInt((res.x - 1) * 0.5f), 0, res.x - 1);
        _originVoxelY = Mathf.Clamp(Mathf.RoundToInt((res.y - 1) * 0.5f), 0, res.y - 1);
        _originVoxelZ = Mathf.Clamp(Mathf.RoundToInt((res.z - 1) * 0.5f), 0, res.z - 1);

        for (int z = 0; z < res.z; z++)
        {
            for (int y = 0; y < res.y; y++)
            {
                for (int x = 0; x < res.x; x++)
                {
                    int i = x + y * res.x + z * res.y * res.x;

                    if (_collisionMask[i])
                    {
                        currentPixels[i] = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                        initialPixels[i] = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                        continue;
                    }

                    voxelDistances[i] = initialPixels[i].g;
                    dynamicMaxCost = Mathf.Max(dynamicMaxCost, initialPixels[i].b);

                    initialPixels[i].g = 0f;
                    initialPixels[i].b = 0f;
                    initialPixels[i].a = 0f;

                    if (growStageDuration <= 0f)
                    {
                        currentPixels[i] = initialPixels[i];
                    }
                    else
                    {
                        currentPixels[i] = new Color(minDensity, 0f, 0f, 0f);
                    }
                    currentPixels[i].g = initialPixels[i].g;
                    currentPixels[i].b = initialPixels[i].b;
                    currentPixels[i].a = initialPixels[i].a;

                    Vector3 pos = MainVoxelGrid.GetGridPosition(x, y, z);
                    voxelDistances[i] = Vector3.Distance(pos, centerPos);
                }
            }
        }

        grid.SetPixels(currentPixels);
        grid.Apply();

        timeCounter = 0.0f;

        Debug.Log("[Smoke Updater] Successfully initialized pixels and cached distances!");
    }

    void Start()
    {
        Debug.Log("[Smoke Updater] Entered Start: " + Time.time);

        // InitializePixels();
    }

    void Update()
    {
        if (MainVoxelGrid == null || MainVoxelGrid.mainGrid == null || currentPixels == null) return;

        if (growStageDuration > 0f && timeCounter < growStageDuration)
        {
            GrowtoInitialSize();
        }
        else if (growStageDuration + smokeDuration > 0f && timeCounter < growStageDuration + smokeDuration)
        {
            SteadyStateEvolution();
        }
        else
        {
            FizzleOut();
        }
        timeCounter += Time.deltaTime;
    }
    void SteadyStateEvolution()
    {
        if (MainVoxelGrid == null || MainVoxelGrid.mainGrid == null) return;

        Vector3Int res = MainVoxelGrid.resolution;
        float dt = Time.deltaTime;

        if (lastFramePixels == null || lastFramePixels.Length != currentPixels.Length)
            lastFramePixels = new Color[currentPixels.Length];

        System.Array.Copy(currentPixels, lastFramePixels, currentPixels.Length);

        for (int z = 0; z < res.z; z++)
        {
            for (int y = 0; y < res.y; y++)
            {
                for (int x = 0; x < res.x; x++)
                {
                    int i = x + y * res.x + z * res.y * res.x;
                    if (_collisionMask[i]) continue;
                    Vector3 vel = new Vector3(lastFramePixels[i].g, lastFramePixels[i].b, lastFramePixels[i].a);

                    float backX = x - vel.x * dt * advectionStrength * res.x;
                    float backY = y - vel.y * dt * advectionStrength * res.y;
                    float backZ = z - vel.z * dt * advectionStrength * res.z;

                    int ix = Mathf.Clamp(Mathf.RoundToInt(backX), 0, res.x - 1);
                    int iy = Mathf.Clamp(Mathf.RoundToInt(backY), 0, res.y - 1);
                    int iz = Mathf.Clamp(Mathf.RoundToInt(backZ), 0, res.z - 1);
                    int prevIdx = ix + iy * res.x + iz * res.y * res.x;

                    if (!_collisionMask[prevIdx])
                    {
                        currentPixels[i].r = Mathf.Min(lastFramePixels[prevIdx].r, initialPixels[i].r);
                    }
                    if (currentPixels[i].r < initialPixels[i].r)
                    {
                        currentPixels[i].r = Mathf.MoveTowards(currentPixels[i].r, initialPixels[i].r, regrowthRate * dt);
                    }

                    currentPixels[i].g = lastFramePixels[i].g * velocityDamping;
                    currentPixels[i].b = lastFramePixels[i].b * velocityDamping;
                    currentPixels[i].a = lastFramePixels[i].a * velocityDamping;

                    if (currentPixels[i].r > densityThreshold)
                    {
                        currentPixels[i].r += Random.Range(-0.01f, 0.02f) * dt;
                        currentPixels[i].r = Mathf.Clamp(currentPixels[i].r, minDensity, initialPixels[i].r);
                    }
                }
            }
        }

        MainVoxelGrid.mainGrid.SetPixels(currentPixels);
        MainVoxelGrid.mainGrid.Apply();
    }
    void FizzleOut()
    {
        Texture3D grid = MainVoxelGrid.mainGrid;

        for (int i = 0; i < currentPixels.Length; i++)
        {
            if (_collisionMask[i])
            {
                currentPixels[i] = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                continue;
            }

            float density = currentPixels[i].r;

            if (density > densityThreshold)
            {
                density += Random.Range(minDensityDelta, maxDensityDelta) * Time.deltaTime;
            }

            currentPixels[i].r = Mathf.Clamp(density, minDensity, maxDensity);
        }

        grid.SetPixels(currentPixels);
        grid.Apply();
    }

    void GrowtoInitialSize()
    {
        Texture3D grid = MainVoxelGrid.mainGrid;

        float globalProgress = Mathf.Clamp01(timeCounter / growStageDuration);
        globalProgress = Mathf.SmoothStep(0.0f, 1.0f, Mathf.SmoothStep(0.0f, 1.0f, globalProgress));
        float currentWaveRadius = globalProgress * dynamicMaxCost;

        float localDuration = growStageDuration * localGrowthRatio;
        Vector3Int res = MainVoxelGrid.resolution;

        for (int z = 0; z < res.z; z++)
        {
            for (int y = 0; y < res.y; y++)
            {
                for (int x = 0; x < res.x; x++)
                {
                    int i = x + y * res.x + z * res.y * res.x;

                    if (_collisionMask[i])
                    {
                        currentPixels[i] = new Color(0, 0, 0, 0);
                        continue;
                    }

                    float density = currentPixels[i].r;
                    float initialDensity = initialPixels[i].r;

                    if (density < initialDensity && voxelDistances[i] <= currentWaveRadius)
                    {
                        bool canReach = HasLineOfSight(
                            _originVoxelX, _originVoxelY, _originVoxelZ, x, y, z);

                        if (canReach)
                        {
                            if (localDuration <= 0f)
                                density = initialDensity;
                            else
                            {
                                float localGrowRate = initialDensity / localDuration;
                                density += localGrowRate * Time.deltaTime;
                            }
                        }
                    }

                    currentPixels[i].r = Mathf.Clamp(density, minDensity, initialDensity);
                }
            }
        }
        grid.SetPixels(currentPixels);
        grid.Apply();
    }


    public bool[] GetCollisionMask()
    {
        return _collisionMask;
    }

    public Color getCurrentDensity(int idx)
    {
        return currentPixels[idx];
    }

    public void AddVelocityAtWorldPos(Vector3 worldPos, Vector3 velocityForce, float radius)
    {
        if (currentPixels == null || MainVoxelGrid == null) return;

        Vector3Int res = MainVoxelGrid.resolution;
        Vector3 center = transform.position;
        Vector3 halfScale = MainVoxelGrid.spaceScale * 0.5f;

        Vector3 localPos = transform.InverseTransformPoint(worldPos);

        int centerX = Mathf.RoundToInt((localPos.x / MainVoxelGrid.spaceScale.x + 0.5f) * res.x);
        int centerY = Mathf.RoundToInt((localPos.y / MainVoxelGrid.spaceScale.y + 0.5f) * res.y);
        int centerZ = Mathf.RoundToInt((localPos.z / MainVoxelGrid.spaceScale.z + 0.5f) * res.z);

        int r = Mathf.CeilToInt(radius);
        for (int z = centerZ - r; z <= centerZ + r; z++)
        {
            for (int y = centerY - r; y <= centerY + r; y++)
            {
                for (int x = centerX - r; x <= centerX + r; x++)
                {
                    if (x >= 0 && x < res.x && y >= 0 && y < res.y && z >= 0 && z < res.z)
                    {
                        int i = x + y * res.x + z * res.y * res.x;

                        float dist = Vector3.Distance(new Vector3(x, y, z), new Vector3(centerX, centerY, centerZ));
                        if (dist <= radius)
                        {
                            float falloff = 1.0f - (dist / radius);
                            currentPixels[i].g += velocityForce.x * falloff * Time.deltaTime;
                            currentPixels[i].b += velocityForce.y * falloff * Time.deltaTime;
                            currentPixels[i].a += velocityForce.z * falloff * Time.deltaTime;
                        }
                    }
                }
            }
        }
    }

    public void AddExplosionForceAtWorldPos(Vector3 worldPos, float force, float radius)
    {
        if (currentPixels == null || MainVoxelGrid == null) return;

        Vector3Int res = MainVoxelGrid.resolution;
        Vector3 localPos = transform.InverseTransformPoint(worldPos);

        int centerX = Mathf.RoundToInt((localPos.x / MainVoxelGrid.spaceScale.x + 0.5f) * res.x);
        int centerY = Mathf.RoundToInt((localPos.y / MainVoxelGrid.spaceScale.y + 0.5f) * res.y);
        int centerZ = Mathf.RoundToInt((localPos.z / MainVoxelGrid.spaceScale.z + 0.5f) * res.z);

        int r = Mathf.CeilToInt(radius);
        for (int z = centerZ - r; z <= centerZ + r; z++)
        {
            for (int y = centerY - r; y <= centerY + r; y++)
            {
                for (int x = centerX - r; x <= centerX + r; x++)
                {
                    if (x >= 0 && x < res.x && y >= 0 && y < res.y && z >= 0 && z < res.z)
                    {
                        int i = x + y * res.x + z * res.y * res.x;
                        Vector3 voxelPos = new Vector3(x, y, z);
                        float dist = Vector3.Distance(voxelPos, new Vector3(centerX, centerY, centerZ));

                        if (dist <= radius && dist > 0.1f)
                        {
                            float falloff = 1.0f - (dist / radius);
                            Vector3 dir = (voxelPos - new Vector3(centerX, centerY, centerZ)).normalized;

                            currentPixels[i].g += dir.x * force * falloff * Time.deltaTime;
                            currentPixels[i].b += dir.y * force * falloff * Time.deltaTime;
                            currentPixels[i].a += dir.z * force * falloff * Time.deltaTime;

                            currentPixels[i].r *= (1.0f - falloff);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns true if there is a clear (non-collision) line of sight 
    /// from voxel (x0,y0,z0) to voxel (x1,y1,z1).
    /// Uses a simple 3D DDA march.
    /// </summary>
    private bool HasLineOfSight(int x0, int y0, int z0, int x1, int y1, int z1)
    {
        Vector3Int res = MainVoxelGrid.resolution;

        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int dz = Mathf.Abs(z1 - z0), sz = z0 < z1 ? 1 : -1;

        int cx = x0, cy = y0, cz = z0;

        int dm = Mathf.Max(dx, dy, dz);
        if (dm == 0) return true;

        // Bresenham 3D
        int p1 = 2 * dy - dx;
        int p2 = 2 * dz - dx;

        // Use parametric stepping along the dominant axis
        float stepX = (x1 - x0) / (float)dm;
        float stepY = (y1 - y0) / (float)dm;
        float stepZ = (z1 - z0) / (float)dm;

        for (int step = 1; step < dm; step++) // exclude endpoints
        {
            int nx = Mathf.RoundToInt(x0 + stepX * step);
            int ny = Mathf.RoundToInt(y0 + stepY * step);
            int nz = Mathf.RoundToInt(z0 + stepZ * step);

            nx = Mathf.Clamp(nx, 0, res.x - 1);
            ny = Mathf.Clamp(ny, 0, res.y - 1);
            nz = Mathf.Clamp(nz, 0, res.z - 1);

            int idx = nx + ny * res.x + nz * res.y * res.x;
            if (_collisionMask[idx]) return false; // wall hit → blocked
        }

        return true;
    }
}
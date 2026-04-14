using UnityEngine;

public class SmokeUpdater : MonoBehaviour
{
    [SerializeField] private float densityThreshold = 0.0001f;
    [SerializeField] private float minDensityDelta = -0.3f;
    [SerializeField] private float maxDensityDelta = 0.0f;
    [SerializeField] private float minDensity = 0.0f;
    [SerializeField] private float maxDensity = 5.0f;


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

    public VoxelGrid MainVoxelGrid;

    void initializePixels()
    {
        Debug.Log("[Smoke Updater] Entered initializePixels: " + Time.time);

        if (MainVoxelGrid == null || MainVoxelGrid.mainGrid == null) return;

        Texture3D grid = MainVoxelGrid.mainGrid;

        initialPixels = grid.GetPixels();
        currentPixels = new Color[initialPixels.Length];
        voxelDistances = new float[initialPixels.Length];

        Vector3 centerPos = transform.position;
        Vector3Int res = MainVoxelGrid.resolution;

        for (int z = 0; z < res.z; z++)
        {
            for (int y = 0; y < res.y; y++)
            {
                for (int x = 0; x < res.x; x++)
                {
                    int i = x + y * res.x + z * res.y * res.x;

                    if (growStageDuration <= 0f)
                    {
                        currentPixels[i].r = initialPixels[i].r;
                    }
                    else
                    {
                        currentPixels[i].r = minDensity;
                    }

                    Vector3 pos = MainVoxelGrid.GetGridPosition(x, y, z);
                    voxelDistances[i] = Vector3.Distance(pos, centerPos);
                }
            }
        }

        grid.SetPixels(currentPixels);
        grid.Apply();

        Debug.Log("[Smoke Updater] Successfully initialized pixels and cached distances!");
    }

    void Start()
    {
        Debug.Log("[Smoke Updater] Entered Start: " + Time.time);

        initializePixels();
        timeCounter = 0.0f;
    }

    void Update()
    {
        if (MainVoxelGrid == null || MainVoxelGrid.mainGrid == null || currentPixels == null) return;

        // Stage 1: When the smoke is growing in size
        if (growStageDuration > 0f && timeCounter < growStageDuration)
        {
            GrowtoInitialSize();
        }
        else
        // Stage 2: when the smoke fizzles out
        {
            RandomWalk();
        }

        timeCounter += Time.deltaTime;
    }

    void RandomWalk()
    {
        Texture3D grid = MainVoxelGrid.mainGrid;

        for (int i = 0; i < currentPixels.Length; i++)
        {
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
        float currentWaveRadius = globalProgress * maxCloudRadius;

        float localDuration = growStageDuration * localGrowthRatio;

        for (int i = 0; i < currentPixels.Length; i++)
        {
            float density = currentPixels[i].r;
            float initialDensity = initialPixels[i].r;

            if (density < initialDensity)
            {
                if (voxelDistances[i] <= currentWaveRadius)
                {
                    if (localDuration <= 0f)
                    {
                        density = initialDensity;
                    }
                    else
                    {
                        float localGrowRate = initialDensity / localDuration;
                        density += localGrowRate * Time.deltaTime;
                    }
                }
            }

            currentPixels[i].r = Mathf.Clamp(density, minDensity, initialDensity);
        }

        grid.SetPixels(currentPixels);
        grid.Apply();
    }
}
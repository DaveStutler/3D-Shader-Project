using System;
using UnityEngine;
using System.Collections.Generic;

public class SmokeGrid : MonoBehaviour
{
    // mainvoxel : (density of smoke, velocity_X, velocity_Y, velocity_Z) 
    public Vector3 spaceScale = new Vector3(10f, 10f, 10f);

    public float densityMulptiplier = 10.0f;

    [Range(0f, 1.0f)]
    [SerializeField] private float voxelFillPercentage = 0.20f;

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
    }

    private void Start()
    {
        // GenerateSample();
        smokeRenderer.SetMaterial(material);
        smokeRenderer.SetMainVoxelGrid(mainVoxelGrid.mainGrid);

        collisionMasker.initialize(mainVoxelGrid);

        _smokeupdater.CollisionVoxelGrid = collisionMasker.GetCollisionVoxel();

        GenerateSampleFloodFill();

        _smokeupdater.InitializePixels();

        if (debugVoxelGrid != null)
        {
            debugVoxelGrid.initialize();
        }
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
                    float maxAllowedRadius = Mathf.Max(spaceScale.x, spaceScale.y, spaceScale.z) * 0.5f;
                    float mask = Mathf.Clamp01(1.0f - (distToCenter / maxAllowedRadius));
                    float finalDensity = noiseValue * mask * densityMulptiplier;

                    // give random speed to the grid
                    Vector3 velocity = Vector3.zero;

                    int idx = x + y * mainVoxelGrid.resolution.x + z * mainVoxelGrid.resolution.y * mainVoxelGrid.resolution.x;
                    colors[idx] = new Color(finalDensity, velocity.x, velocity.y, velocity.z);
                }
            }
        }

        mainVoxelGrid.mainGrid.SetPixels(colors);
        mainVoxelGrid.mainGrid.Apply();
    }

    public void GenerateSampleFloodFillOld(float noiseScale = 0.2f)
    {
        int resX = (int)mainVoxelGrid.resolution.x;
        int resY = (int)mainVoxelGrid.resolution.y;
        int resZ = (int)mainVoxelGrid.resolution.z;

        Vector3Int emissionCenter = new Vector3Int(
            (int)(resX / 2),
            (int)(resY / 2),
            (int)(resZ / 2)
        );

        int totalVoxels = resX * resY * resZ;
        int maxVoxels = (int)(voxelFillPercentage * totalVoxels);

        Color[] colors = new Color[totalVoxels];
        bool[] visited = new bool[totalVoxels];
        Color[] collisionData = _smokeupdater.CollisionVoxelGrid.mainGrid.GetPixels();

        MinHeapQueue<Vector3Int, float> frontier = new MinHeapQueue<Vector3Int, float>();

        if (emissionCenter.x < 0 || emissionCenter.x >= resX ||
            emissionCenter.y < 0 || emissionCenter.y >= resY ||
            emissionCenter.z < 0 || emissionCenter.z >= resZ)
        {
            Debug.LogWarning("Flood fill emissionCenter is outside the voxel grid bounds.");
            return;
        }

        frontier.Enqueue(emissionCenter, 0f);
        int startIdx = emissionCenter.x + emissionCenter.y * resX + emissionCenter.z * resY * resX;
        visited[startIdx] = true;

        int voxelsFilled = 0;

        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        };

        // Calculates how "expensive" a voxel is based on its position relative to the center.
        float CalculateCost(Vector3Int pos)
        {
            float dx = pos.x - emissionCenter.x;
            float dy = pos.y - emissionCenter.y;
            float dz = pos.z - emissionCenter.z;

            // Adjust these to shape your oblong sphere
            // Lower multiplier = cheaper to move there (STRETCHES the cloud)
            // Higher multiplier = harder to move there (SQUISHES the cloud)
            float horizontalSquish = 1.0f;   // Neutral horizontal circle
            float upwardSquish = 2.5f;       // High cost to go up (flattens the top)
            float downwardStretch = 0.4f;    // Low cost to go down (pulls the bottom down)

            float verticalWeight = (dy > 0) ? upwardSquish : downwardStretch;

            return (dx * dx * horizontalSquish) + (dy * dy * verticalWeight) + (dz * dz * horizontalSquish);
        }

        while (frontier.Count > 0 && voxelsFilled < maxVoxels)
        {
            Vector3Int current = frontier.Dequeue();
            int currentIdx = current.x + current.y * resX + current.z * resY * resX;

            float px = (current.x + 100.5f) * noiseScale;
            float py = (current.y + 100.5f) * noiseScale;
            float pz = (current.z + 100.5f) * noiseScale;

            float noiseValue = Mathf.PerlinNoise(px, py) * Mathf.PerlinNoise(py, pz);
            float finalDensity = noiseValue * densityMulptiplier;

            colors[currentIdx] = new Color(finalDensity, 0, 0, 0);
            voxelsFilled++;

            foreach (Vector3Int dir in directions)
            {
                Vector3Int neighbor = current + dir;

                if (neighbor.x >= 0 && neighbor.x < resX &&
                    neighbor.y >= 0 && neighbor.y < resY &&
                    neighbor.z >= 0 && neighbor.z < resZ)
                {
                    int neighborIdx = neighbor.x + neighbor.y * resX + neighbor.z * resY * resX;

                    if (!visited[neighborIdx])
                    {
                        visited[neighborIdx] = true;

                        if (collisionData[neighborIdx].r > 0.1f) continue; // Blocked by map geometry

                        float cost = CalculateCost(neighbor);
                        frontier.Enqueue(neighbor, cost);
                    }
                }
            }
        }

        mainVoxelGrid.mainGrid.SetPixels(colors);
        mainVoxelGrid.mainGrid.Apply();
    }

    public void GenerateSampleFloodFill(float noiseScale = 0.2f)
    {
        int resX = (int)mainVoxelGrid.resolution.x;
        int resY = (int)mainVoxelGrid.resolution.y;
        int resZ = (int)mainVoxelGrid.resolution.z;

        Vector3Int emissionCenter = new Vector3Int(resX / 2, resY / 2, resZ / 2);

        int totalVoxels = resX * resY * resZ;
        int maxVoxels = (int)(voxelFillPercentage * totalVoxels);

        Color[] colors = new Color[totalVoxels];
        bool[] visited = new bool[totalVoxels];
        Color[] collisionData = _smokeupdater.CollisionVoxelGrid.mainGrid.GetPixels();

        MinHeapQueue<Vector3Int, float> frontier = new MinHeapQueue<Vector3Int, float>();

        if (emissionCenter.x < 0 || emissionCenter.x >= resX ||
            emissionCenter.y < 0 || emissionCenter.y >= resY ||
            emissionCenter.z < 0 || emissionCenter.z >= resZ) return;

        float CalculateCost(Vector3Int pos)
        {
            float dx = pos.x - emissionCenter.x;
            float dy = pos.y - emissionCenter.y;
            float dz = pos.z - emissionCenter.z;

            float horizontalSquish = 1.0f;
            float upwardSquish = 2.5f;
            float downwardStretch = 0.4f;
            float verticalWeight = (dy > 0) ? upwardSquish : downwardStretch;

            return (dx * dx * horizontalSquish) + (dy * dy * verticalWeight) + (dz * dz * horizontalSquish);
        }

        frontier.Enqueue(emissionCenter, 0f);
        visited[emissionCenter.x + emissionCenter.y * resX + emissionCenter.z * resY * resX] = true;

        List<Vector3Int> validVoxels = new List<Vector3Int>(maxVoxels);
        float maxCostReached = 0f;

        Vector3Int[] directions = new Vector3Int[] {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        };

        while (frontier.Count > 0 && validVoxels.Count < maxVoxels)
        {
            Vector3Int current = frontier.Dequeue();
            validVoxels.Add(current);

            float currentCost = CalculateCost(current);
            if (currentCost > maxCostReached) maxCostReached = currentCost;

            foreach (Vector3Int dir in directions)
            {
                Vector3Int neighbor = current + dir;

                if (neighbor.x >= 0 && neighbor.x < resX &&
                    neighbor.y >= 0 && neighbor.y < resY &&
                    neighbor.z >= 0 && neighbor.z < resZ)
                {
                    int neighborIdx = neighbor.x + neighbor.y * resX + neighbor.z * resY * resX;

                    if (!visited[neighborIdx])
                    {
                        visited[neighborIdx] = true;

                        if (collisionData[neighborIdx].r > 0.1f) continue;

                        float cost = CalculateCost(neighbor);
                        frontier.Enqueue(neighbor, cost);
                    }
                }
            }
        }

        foreach (Vector3Int voxel in validVoxels)
        {
            int idx = voxel.x + voxel.y * resX + voxel.z * resY * resX;
            float voxelCost = CalculateCost(voxel);

            float mask = Mathf.Clamp01(1.0f - (voxelCost / (maxCostReached + Mathf.Epsilon)));
            mask = Mathf.SmoothStep(0, 1, mask);

            Vector3 pos = mainVoxelGrid.GetGridPosition(voxel.x, voxel.y, voxel.z);
            float px = pos.x * noiseScale;
            float py = pos.y * noiseScale;
            float pz = pos.z * noiseScale;

            float noiseValue = Mathf.PerlinNoise(px, py) * Mathf.PerlinNoise(py, pz);
            float finalDensity = noiseValue * mask * densityMulptiplier;

            colors[idx] = new Color(finalDensity, voxelCost, maxCostReached, 0f);
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

// A lightweight, array-based Binary Min-Heap
public class MinHeapQueue<TItem, TPriority> where TPriority : IComparable<TPriority>
{
    private List<(TItem Item, TPriority Priority)> _elements = new List<(TItem, TPriority)>();

    public int Count => _elements.Count;

    public void Enqueue(TItem item, TPriority priority)
    {
        _elements.Add((item, priority));
        int i = _elements.Count - 1;

        // Sift Up
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (_elements[i].Priority.CompareTo(_elements[parent].Priority) >= 0) break;

            var tmp = _elements[i];
            _elements[i] = _elements[parent];
            _elements[parent] = tmp;
            i = parent;
        }
    }

    public TItem Dequeue()
    {
        if (_elements.Count == 0) throw new InvalidOperationException("Queue is empty.");

        TItem result = _elements[0].Item;
        _elements[0] = _elements[_elements.Count - 1];
        _elements.RemoveAt(_elements.Count - 1);

        int i = 0;

        // Sift Down
        while (true)
        {
            int leftChild = 2 * i + 1;
            if (leftChild >= _elements.Count) break;

            int rightChild = leftChild + 1;
            int minChild = leftChild;

            if (rightChild < _elements.Count && _elements[rightChild].Priority.CompareTo(_elements[leftChild].Priority) < 0)
            {
                minChild = rightChild;
            }

            if (_elements[i].Priority.CompareTo(_elements[minChild].Priority) <= 0) break;

            var tmp = _elements[i];
            _elements[i] = _elements[minChild];
            _elements[minChild] = tmp;
            i = minChild;
        }

        return result;
    }
}
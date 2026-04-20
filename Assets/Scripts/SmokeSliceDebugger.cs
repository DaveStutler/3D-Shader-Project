using UnityEngine;

public class SmokeSliceDebugger : MonoBehaviour
{
    [Header("References")]
    public SmokeUpdater smokeUpdater;
    public SmokeGrid smokeGrid;

    [Header("Debug Controls")]
    public bool showDebug = true;

    public enum SliceAxis { X, Y, Z }
    public SliceAxis sliceAxis = SliceAxis.Y;

    [Range(0, 256)]
    public int sliceIndex = 0;

    [Header("Visual Settings")]
    public float maxVisualDensity = 5.0f;

    [Range(0.1f, 1.0f)]
    public float voxelGapScale = 0.95f;

    private void OnDrawGizmos()
    {
        if (!showDebug || !Application.isPlaying) return;
        if (smokeUpdater == null || smokeGrid == null) return;

        var mainGrid = smokeGrid.GetMainGrid();
        if (mainGrid == null) return;

        bool[] collisionMask = smokeUpdater.GetCollisionMask();
        if (collisionMask == null || collisionMask.Length == 0) return;

        Vector3Int res = mainGrid.resolution;
        Vector3 maxVoxelSize = mainGrid.GetGridSize() * voxelGapScale;

        int maxIndex = (sliceAxis == SliceAxis.X) ? res.x : (sliceAxis == SliceAxis.Y) ? res.y : res.z;
        int currentSlice = Mathf.Clamp(sliceIndex, 0, maxIndex - 1);

        int startX = (sliceAxis == SliceAxis.X) ? currentSlice : 0;
        int endX = (sliceAxis == SliceAxis.X) ? currentSlice + 1 : res.x;

        int startY = (sliceAxis == SliceAxis.Y) ? currentSlice : 0;
        int endY = (sliceAxis == SliceAxis.Y) ? currentSlice + 1 : res.y;

        int startZ = (sliceAxis == SliceAxis.Z) ? currentSlice : 0;
        int endZ = (sliceAxis == SliceAxis.Z) ? currentSlice + 1 : res.z;

        for (int z = startZ; z < endZ; z++)
        {
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    int idx = x + y * res.x + z * res.x * res.y;

                    if (idx >= collisionMask.Length) continue;

                    Vector3 pos = mainGrid.GetGridPosition(x, y, z);

                    if (collisionMask[idx])
                    {
                        Gizmos.color = Color.black;
                        Gizmos.DrawCube(pos, maxVoxelSize);
                    }
                    else
                    {
                        float density = smokeUpdater.getCurrentDensity(idx).r;

                        if (density > 0.001f) 
                        {
                            float scaleFraction = Mathf.Clamp01(density / maxVisualDensity);

                            Gizmos.color = Color.white;
                            Gizmos.DrawCube(pos, maxVoxelSize * scaleFraction);
                        }
                    }
                }
            }
        }
    }
}
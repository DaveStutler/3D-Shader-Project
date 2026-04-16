using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CollisionMasker : MonoBehaviour
{
    public int CollisionResoltion = 64;
    public LayerMask obstacleLayer;

    private VoxelGrid _mainGridRef;
    private VoxelGrid _collisionVoxelGrid;

    public void initialize(VoxelGrid gridRef)
    {
        _mainGridRef = gridRef;

        ExecuteDetection();
    }

    public VoxelGrid GetCollisionVoxel()
    {
        return _collisionVoxelGrid;
    }

    void ExecuteDetection()
    {

        Collider[] hitObstacles = 
            Physics.OverlapBox(
                transform.position,
                _mainGridRef.spaceScale / 2.0f,
                transform.rotation,
                obstacleLayer
            );

        if (hitObstacles.Length == 0)
        {
            return;
        }

        int layer = 1;
        int max_layer = 1;
        
        while (Mathf.Pow(2, max_layer) < CollisionResoltion)
        {
            max_layer++;
        }

        bool[][] lookup_tables = new bool[max_layer][];

        Vector3 box_corner = transform.position
                 - transform.right * _mainGridRef.spaceScale.x * 0.5f
                 - transform.up * _mainGridRef.spaceScale.y * 0.5f
                 - transform.forward * _mainGridRef.spaceScale.z * 0.5f;

        while (layer <= max_layer)
        {
            int grid_num = (int)Mathf.Pow(2, layer);
            
            lookup_tables[layer - 1] = new bool[grid_num * grid_num * grid_num];

            float radius = Mathf.Max(_mainGridRef.spaceScale.x, _mainGridRef.spaceScale.y, _mainGridRef.spaceScale.z) / Mathf.Pow(2.0f, layer+1) * 1.73206f;
            
            for (int x = 0; x < grid_num; x++)
            {
                for (int y = 0; y < grid_num; y++)
                {
                    for (int z = 0; z < grid_num; z++)
                    {
                        if (layer > 1 && lookup_tables[layer - 2][x / 2 + y / 2 * (grid_num / 2) + z / 2 * (grid_num / 2) * (grid_num / 2)] == false)
                        {
                            lookup_tables[layer - 1][x + y * grid_num + z * grid_num * grid_num] = false;
                            continue;
                        }
                        else
                        {
                            Vector3 pos = box_corner;
                            pos += (x + 0.5f) / (float)grid_num * transform.right * _mainGridRef.spaceScale.x;
                            pos += (y + 0.5f) / (float)grid_num * transform.up * _mainGridRef.spaceScale.y;
                            pos += (z + 0.5f) / (float)grid_num * transform.forward * _mainGridRef.spaceScale.z;

                            bool is_collided = false;

                            foreach (Collider col in hitObstacles)
                            {
                                if ((col.ClosestPoint(pos) - pos).magnitude < radius)
                                {
                                    is_collided = true; break;
                                }
                            }

                            lookup_tables[layer - 1][x + y * grid_num + z * grid_num * grid_num] = is_collided;
                        }
                    }
                }
            }

            layer++;
        }


        TriLerp2Voxelmask(lookup_tables[max_layer - 1], (int)Mathf.Pow(2, max_layer));
    }

    // currently in cpu but the lerp will be done by gpu itself in shader
    void TriLerp2Voxelmask(bool[] lookupTable, int resolution)
    {
        Vector3Int targetRes = _mainGridRef.resolution;

        Color[] colors = new Color[targetRes.x * targetRes.y * targetRes.z];
        for (int z = 0; z < targetRes.z; z++)
        {
            int _z = (int)(z * resolution) / targetRes.z;
            for (int y = 0; y < targetRes.y; y++)
            {
                int _y = (int)(y * resolution) / targetRes.y;
                for (int x = 0; x < targetRes.x; x++)
                {
                    int _x = (int)(x * resolution) / targetRes.x;

                    bool result = lookupTable[_x + _y * resolution + _z * resolution * resolution];

                    if (result) colors[x + y * targetRes.x + z * targetRes.x * targetRes.y] = new Color(1.0f, 0.0f, 0.0f, 0.0f);
                    else colors[x + y * targetRes.x + z * targetRes.x * targetRes.y] = new Color();
                }
            }
        }

        _collisionVoxelGrid = gameObject.AddComponent<VoxelGrid>();
        _collisionVoxelGrid.resolution = _mainGridRef.resolution;
        _collisionVoxelGrid.spaceScale = _mainGridRef.spaceScale;

        _collisionVoxelGrid.mainGrid = new Texture3D(targetRes.x, targetRes.y, targetRes.z, TextureFormat.R8, false);
        _collisionVoxelGrid.mainGrid.wrapMode = TextureWrapMode.Clamp;
        _collisionVoxelGrid.mainGrid.filterMode = FilterMode.Point;

        _collisionVoxelGrid.mainGrid.SetPixels(colors);
        _collisionVoxelGrid.mainGrid.Apply();
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}

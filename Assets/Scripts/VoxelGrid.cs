using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class VoxelGrid : MonoBehaviour
{

    public Texture3D mainGrid;
    public TextureFormat mainGridTexFormat = TextureFormat.RGBAFloat;

    public List<Texture3D> extraGrid;

    public Vector3Int resolution = new Vector3Int(32, 32, 32);

    // default 1 means the grid is filled with 1 * 1 * 1 space;
    public Vector3 spaceScale = new Vector3(10f, 10f, 10f);

    public bool isStatic = true;

    // public bool showGizmos = true;
    // public bool drawOnlyBounds = true;

    private List<List<List<Vector3>>> _baked_position;

    private int _resolution_x;
    private int _resolution_y;
    private int _resolution_z;

    void Start()
    {
        ;
    }

    void Update()
    {
        
    }

    public void initGrid()
    {
        _resolution_x = resolution.x >= 0 ? resolution.x : 0;
        _resolution_y = resolution.y >= 0 ? resolution.y : 0;
        _resolution_z = resolution.z >= 0 ? resolution.z : 0;

        mainGrid = new Texture3D(_resolution_x, _resolution_y, _resolution_z, mainGridTexFormat, false);
        mainGrid.wrapMode = TextureWrapMode.Clamp;
        mainGrid.filterMode = FilterMode.Trilinear;
    }

    public Vector3 GetGridPosition(int x, int y, int z)
    {
        int resX = Mathf.Max(_resolution_x, resolution.x, 1);
        int resY = Mathf.Max(_resolution_y, resolution.y, 1);
        int resZ = Mathf.Max(_resolution_z, resolution.z, 1);

        x = Mathf.Clamp(x, 0, resX - 1);
        y = Mathf.Clamp(y, 0, resY - 1);
        z = Mathf.Clamp(z, 0, resZ - 1);

        Vector3 center = transform.position;

        Vector3 corner = center
                         - transform.right * spaceScale.x * 0.5f
                         - transform.up * spaceScale.y * 0.5f
                         - transform.forward * spaceScale.z * 0.5f;

        return corner
            + (x + 0.5f) / (float)resX * transform.right * spaceScale.x
            + (y + 0.5f) / (float)resY * transform.up * spaceScale.y
            + (z + 0.5f) / (float)resZ * transform.forward * spaceScale.z;
    }

    private void OnDrawGizmos()
    {
        // moved to the smoke grid
    }
}

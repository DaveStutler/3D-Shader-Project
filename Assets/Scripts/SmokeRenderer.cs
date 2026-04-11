using NUnit.Framework.Internal;
using UnityEngine;

public class SmokeRenderer : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    public void Init(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        // use a box mesh
        transform.localScale = scale;
        transform.rotation = rotation;
        transform.position = position;
    }

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        
    }

    public void SetMaterial(Material mat)
    {
        meshRenderer.sharedMaterial = mat;
    }

    public void SetMainVoxelGrid(Texture3D texture3D)
    {
        meshRenderer.sharedMaterial.SetTexture("_MainVoxelTex", texture3D);
    }
}

using NUnit.Framework.Internal;
using UnityEngine;

public class SmokeRenderer : MonoBehaviour
{
    private Material _instanceMaterial;
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
        _instanceMaterial = new Material(mat);
        meshRenderer.sharedMaterial = _instanceMaterial;
    }

    public void SetMainVoxelGrid(Texture3D texture3D)
    {
        if (_instanceMaterial != null)
        {
            _instanceMaterial.SetTexture("_MainVoxelTex", texture3D);
        }
    }

    void OnDestroy()
    {
        if (_instanceMaterial != null)
        {
            Destroy(_instanceMaterial);
        }
    }
}

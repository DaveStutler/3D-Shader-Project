using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class VolumeGenerator : MonoBehaviour
{
    public int resolution = 256;
    public float noiseScale = 0.1f;
    public float densityMultiplier = 2.0f;

    private Texture3D volumeTexture;
    private Material mat;

    void Start()
    {
        GenerateVolume();
    }

    void GenerateVolume()
    {
        volumeTexture = new Texture3D(resolution, resolution, resolution, TextureFormat.RFloat, false);
        volumeTexture.wrapMode = TextureWrapMode.Clamp;
        volumeTexture.filterMode = FilterMode.Bilinear;

        Color[] colors = new Color[resolution * resolution * resolution];

        for (int z = 0; z < resolution; z++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float px = x * noiseScale;
                    float py = y * noiseScale;
                    float pz = z * noiseScale;

                    float noiseValue = Mathf.PerlinNoise(px, py) * Mathf.PerlinNoise(py, pz);

                    Vector3 pos = new Vector3(x, y, z) / resolution;
                    float distToCenter = Vector3.Distance(pos, new Vector3(0.5f, 0.5f, 0.5f));
                    float mask = Mathf.Clamp01(1.0f - distToCenter * 2.0f); 

                    float finalDensity = noiseValue * mask * densityMultiplier;

                    int idx = x + y * resolution + z * resolution * resolution;
                    colors[idx] = new Color(finalDensity, 0, 0, 0);
                }
            }
        }

        volumeTexture.SetPixels(colors);
        volumeTexture.Apply();

        mat = GetComponent<MeshRenderer>().material;
        mat.SetTexture("_VolumeTex", volumeTexture);
    }

    void OnDestroy()
    {
        if (volumeTexture != null) Destroy(volumeTexture);
    }
}
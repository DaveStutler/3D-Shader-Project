using UnityEngine;

public class SmokeUpdater : MonoBehaviour
{
    // this should be a temp script, all the things happening here will be transfered 
    // to gpu as compute shader, but it's good to test algorithm here using cpu loops
    public VoxelGrid MainVoxelGrid; 
    void Start()
    {
        
    }

    void Update()
    {
        RandomWalk();
    }

    void RandomWalk()
    {
        if (MainVoxelGrid == null || MainVoxelGrid.mainGrid == null) return;

        Texture3D grid = MainVoxelGrid.mainGrid;

        Color[] pixels = grid.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            float density = pixels[i].r;

            if (density > 0.0001f)
            {
                density += Random.Range(-0.3f, 0.0f) * Time.deltaTime;
            }

            pixels[i].r = Mathf.Clamp(density, 0f, 10f);
        }

        grid.SetPixels(pixels);
        grid.Apply();
    }

}

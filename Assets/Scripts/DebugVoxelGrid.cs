using NUnit.Framework;
using UnityEngine;

public class DebugVoxelGrid : MonoBehaviour
{
    public SmokeUpdater smokeUpdater;
    public SmokeGrid smokeGrid;

    // enable before start
    public bool showDebug = false;

    // public int DebugResolution = 64;

    [SerializeField] private GameObject gridUnit;
    [SerializeField] private GameObject maskedGridUnit;

    private bool[] _copyed_collision_mask;

    Transform[] units;

    void Start()
    {
    }

    public void initialize()
    {
        if (showDebug)
        {
            _copyed_collision_mask = smokeUpdater.GetCollisionMask();

            var mainGrid = smokeGrid.GetMainGrid();

            units = new Transform[_copyed_collision_mask.Length];

            float unitscale = (smokeGrid.spaceScale.x / smokeGrid.GetMainGrid().resolution.x);

            for (int i = 0; i < units.Length; i++)
            {
                int idx = i;

                int _z = idx % (mainGrid.resolution.x * mainGrid.resolution.y);
                idx -= _z * (mainGrid.resolution.x * mainGrid.resolution.y);

                int _y = idx % mainGrid.resolution.x;
                idx -= _y * mainGrid.resolution.x;

                int _x = idx;

                if (_copyed_collision_mask[i])
                {
                    units[i] = GameObject.Instantiate(maskedGridUnit, mainGrid.GetGridPosition(_x,_y,_z), smokeGrid.transform.rotation).transform;
                    units[i].parent = transform;
                    units[i].localScale = Vector3.one * unitscale;
                }
                else
                {
                    units[i] = GameObject.Instantiate(maskedGridUnit, mainGrid.GetGridPosition(_x, _y, _z), smokeGrid.transform.rotation).transform;
                    units[i].parent = transform;
                    units[i].localScale = Vector3.zero;
                }
            }
        }
    }

    void Update()
    {
        if (showDebug)
        {
            var mainGrid = smokeGrid.GetMainGrid();

            float unitscale = (smokeGrid.spaceScale.x / mainGrid.resolution.x);

            for (int i = 0; i < units.Length; i++)
            {

                if (_copyed_collision_mask[i])
                {
                    ;
                }
                else
                {
                    units[i].localScale = smokeUpdater.getCurrentDensity(i).r * Vector3.one * unitscale;
                }
            }
        }
    }
}

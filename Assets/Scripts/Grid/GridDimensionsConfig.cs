using Unity.Mathematics;
using UnityEngine;

public class GridDimensionsConfig : MonoBehaviour
{
    public static GridDimensionsConfig Instance;
    public int2 GridSize;

    private void Awake()
    {
        Instance = this;
    }
}
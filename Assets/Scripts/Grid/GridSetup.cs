using UnityEngine;

public class GridSetup : MonoBehaviour
{
    private const int Width = 30;
    private const int Height = 15;
    private const float CellSize = 1f;
    [SerializeField] private PathfindingVisual _pathfindingVisual;
    public Grid<GridPath> PathGrid;
    public Grid<GridDamageable> DamageableGrid;

    private bool _shouldSetToWalkableOnMouseDown;
    public static GridSetup Instance { private set; get; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        PathGrid = new Grid<GridPath>(Width, Height, CellSize, Vector3.zero, ( grid,  x,  y) => new GridPath(grid, x, y));
        DamageableGrid = new Grid<GridDamageable>(Width, Height, CellSize, Vector3.zero, (grid, x, y) => new GridDamageable(grid, x, y));

        // TODO: Is it a problem that we're using a specific grid to control the visual updates?
        _pathfindingVisual.SetGrid(PathGrid, DamageableGrid);
    }
}
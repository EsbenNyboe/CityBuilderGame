using UnityEngine;

public class GridSetup : MonoBehaviour
{
    private const float CellSize = 1f;

    [SerializeField] private int _width = 60;

    [SerializeField] private int _height = 30;

    [SerializeField] private PathfindingVisual _pathfindingVisual;
    public Grid<GridPath> PathGrid;
    public Grid<GridDamageable> DamageableGrid;
    public Grid<GridOccupation> OccupationGrid;

    private bool _shouldSetToWalkableOnMouseDown;
    public static GridSetup Instance { private set; get; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        PathGrid = new Grid<GridPath>(_width, _height, CellSize, Vector3.zero, ( grid,  x,  y) => new GridPath(grid, x, y));
        DamageableGrid = new Grid<GridDamageable>(_width, _height, CellSize, Vector3.zero, (grid, x, y) => new GridDamageable(grid, x, y));
        OccupationGrid = new Grid<GridOccupation>(_width, _height, CellSize, Vector3.zero, (grid, x, y) => new GridOccupation(grid, x, y));

        // TODO: Is it a problem that we're using a specific grid to control the visual updates?
        _pathfindingVisual.SetGrid(PathGrid, DamageableGrid, OccupationGrid);
    }
}
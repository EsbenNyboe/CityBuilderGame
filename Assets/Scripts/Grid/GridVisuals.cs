using CodeMonkey.Utils;
using UnityEngine;
using UnityEngine.Serialization;

public class GridVisuals : MonoBehaviour
{
    [SerializeField] private MeshFilter _pathGridMeshFilter;

    [FormerlySerializedAs("_damageableGridMeshFilter")] [SerializeField]
    private MeshFilter _healthBarMeshFilter;

    [SerializeField] private MeshFilter _occupationGridMeshFilter;
    private Mesh _damageableGridMesh;

    private TextMesh[,] _debugTextArray;
    private Grid<GridDamageable> _gridDamageable;
    private Grid<GridOccupation> _gridOccupation;

    private Grid<GridPath> _gridPath;
    private Mesh _occupationGridMesh;

    private Mesh _pathGridMesh;
    private bool _updateHealthBar;
    private bool _updateOccupationDebugMesh;

    private bool _updatePathMesh;
    private bool _updateText;
    private int[] healthBarTriangles;
    private Vector2[] healthBarUv;

    private Vector3[] healthBarVertices;
    private int[] occupationDebugMeshTriangles;
    private Vector2[] occupationDebugMeshUv;

    private Vector3[] occupationDebugMeshVertices;
    private int[] pathMeshTriangles;
    private Vector2[] pathMeshUv;

    private Vector3[] pathMeshVertices;

    private void Awake()
    {
        _pathGridMesh = new Mesh();
        _pathGridMeshFilter.mesh = _pathGridMesh;
        _damageableGridMesh = new Mesh();
        _healthBarMeshFilter.mesh = _damageableGridMesh;
        _occupationGridMesh = new Mesh();
        _occupationGridMeshFilter.mesh = _occupationGridMesh;
    }

    private void LateUpdate()
    {
        if (_updatePathMesh)
        {
            _updatePathMesh = false;
            UpdateVisual();
        }

        if (_updateText)
        {
            _updateText = false;
            UpdateTextVisual();
        }

        if (_updateHealthBar)
        {
            _updateHealthBar = false;
            UpdateHealthBarVisuals();
        }

        if (_updateOccupationDebugMesh)
        {
            _updateOccupationDebugMesh = false;
            UpdateOccupationDebugMesh();
        }
    }

    public void SetGrid(Grid<GridPath> gridPath, Grid<GridDamageable> gridDamageable, Grid<GridOccupation> gridOccupation)
    {
        _gridPath = gridPath;
        _gridDamageable = gridDamageable;
        _gridOccupation = gridOccupation;


        MeshUtils.CreateEmptyMeshArrays(_gridDamageable.GetWidth() * _gridDamageable.GetHeight(), out pathMeshVertices, out pathMeshUv,
            out pathMeshTriangles);
        MeshUtils.CreateEmptyMeshArrays(_gridDamageable.GetWidth() * _gridDamageable.GetHeight(), out healthBarVertices, out healthBarUv,
            out healthBarTriangles);
        MeshUtils.CreateEmptyMeshArrays(_gridDamageable.GetWidth() * _gridDamageable.GetHeight(), out occupationDebugMeshVertices,
            out occupationDebugMeshUv, out occupationDebugMeshTriangles);

        UpdateVisual();
        UpdateHealthBarVisuals();
        UpdateOccupationDebugMesh();

        gridPath.OnGridObjectChanged += Grid_OnGridValueChanged;
        gridDamageable.OnGridObjectChanged += Grid_OnGridValueChanged;
        gridOccupation.OnGridObjectChanged += Grid_OnGridValueChanged;
    }

    private void Grid_OnGridValueChanged(object sender, Grid<GridPath>.OnGridObjectChangedEventArgs e)
    {
        _updatePathMesh = true;
    }

    private void Grid_OnGridValueChanged(object sender, Grid<GridDamageable>.OnGridObjectChangedEventArgs e)
    {
        _updateText = true;
        _updateHealthBar = true;
    }

    private void Grid_OnGridValueChanged(object sender, Grid<GridOccupation>.OnGridObjectChangedEventArgs e)
    {
        _updateOccupationDebugMesh = true;
    }

    private void UpdateVisual()
    {
        for (var x = 0; x < _gridPath.GetWidth(); x++)
        {
            for (var y = 0; y < _gridPath.GetHeight(); y++)
            {
                var index = x * _gridPath.GetHeight() + y;
                var quadSize = new Vector3(1, 1) * _gridPath.GetCellSize();

                var gridNode = _gridPath.GetGridObject(x, y);

                var uv00 = new Vector2(0, 0);
                var uv11 = new Vector2(.5f, .5f);

                if (!gridNode.IsWalkable())
                {
                    // TODO: Insert tree graphic here? 
                    //quadSize = Vector3.zero;
                    uv00 = new Vector2(.5f, .5f);
                    uv11 = new Vector2(1f, 1f);
                }

                MeshUtils.AddToMeshArrays(pathMeshVertices, pathMeshUv, pathMeshTriangles, index, _gridPath.GetWorldPosition(x, y) + quadSize * .0f,
                    0f, quadSize, uv00,
                    uv11);
            }
        }

        _pathGridMesh.vertices = pathMeshVertices;
        _pathGridMesh.uv = pathMeshUv;
        _pathGridMesh.triangles = pathMeshTriangles;
    }

    private void UpdateTextVisual()
    {
        return;
        if (_debugTextArray == default)
        {
            _debugTextArray = new TextMesh[_gridDamageable.GetWidth(), _gridDamageable.GetHeight()];

            for (var x = 0; x < _gridDamageable.GetWidth(); x++)
            {
                for (var y = 0; y < _gridDamageable.GetHeight(); y++)
                {
                    var tileDamageable = _gridDamageable.GetGridObject(x, y);
                    var tileHealth = tileDamageable.IsDamageable() ? ((int)tileDamageable.GetHealth()).ToString() : "";
                    var cellSize = _gridDamageable.GetCellSize();
                    _debugTextArray[x, y] = UtilsClass.CreateWorldText(tileHealth, null,
                        _gridDamageable.GetWorldPosition(x, y) + new Vector3(cellSize, cellSize) * .5f, 30, Color.white, TextAnchor.MiddleCenter);
                }
            }
        }
        else
        {
            for (var x = 0; x < _gridDamageable.GetWidth(); x++)
            {
                for (var y = 0; y < _gridDamageable.GetHeight(); y++)
                {
                    var tileDamageable = _gridDamageable.GetGridObject(x, y);
                    var tileHealth = tileDamageable.IsDamageable() ? ((int)tileDamageable.GetHealth()).ToString() : "";
                    _debugTextArray[x, y].text = tileHealth;
                }
            }
        }
    }

    private void UpdateHealthBarVisuals()
    {
        for (var x = 0; x < _gridDamageable.GetWidth(); x++)
        {
            for (var y = 0; y < _gridDamageable.GetHeight(); y++)
            {
                var index = x * _gridDamageable.GetHeight() + y;
                var gridDamageableObject = _gridDamageable.GetGridObject(x, y);

                var quadWidth = gridDamageableObject.GetHealth() / gridDamageableObject.GetMaxHealth();
                var quadHeight = 0.5f;
                var quadSize = gridDamageableObject.IsDamageable()
                    ? new Vector3(quadWidth, quadHeight) * _gridDamageable.GetCellSize()
                    : Vector3.zero;

                var green = 0.9f;
                var yellow = 0.4f;
                var red = 0.1f;
                var color = quadWidth switch
                {
                    > 0.99f => green,
                    > 0.4f => yellow,
                    _ => red
                };

                var uv00 = new Vector3(color, 0f);
                var uv11 = new Vector3(color, 1f);

                var position = _gridDamageable.GetWorldPosition(x, y);
                // TODO: Make positioning cleaner? Not accounting for cell-size right now...
                position.y += quadHeight / 2;
                if (quadWidth < 1)
                {
                    position.x -= (1 - quadWidth) / 2;
                }

                MeshUtils.AddToMeshArrays(healthBarVertices, healthBarUv, healthBarTriangles, index, position + quadSize * .0f, 0f, quadSize, uv00,
                    uv11);
            }
        }

        _damageableGridMesh.vertices = healthBarVertices;
        _damageableGridMesh.uv = healthBarUv;
        _damageableGridMesh.triangles = healthBarTriangles;
    }

    private void UpdateOccupationDebugMesh()
    {
        if (!DebugGlobals.ShowOccupationGrid())
        {
            _occupationGridMesh = new Mesh();
            _occupationGridMeshFilter.mesh = _occupationGridMesh;
            return;
        }

        for (var x = 0; x < _gridOccupation.GetWidth(); x++)
        {
            for (var y = 0; y < _gridOccupation.GetHeight(); y++)
            {
                var index = x * _gridOccupation.GetHeight() + y;
                var occupationCell = _gridOccupation.GetGridObject(x, y);

                var quadWidth = 0.5f;
                var quadHeight = 0.5f;
                var quadSize = occupationCell.IsOccupied()
                    ? new Vector3(quadWidth, quadHeight) * _gridOccupation.GetCellSize()
                    : Vector3.zero;

                var colorRed = 0.1f;
                var uv00 = new Vector3(colorRed, 0f);
                var uv11 = new Vector3(colorRed, 1f);

                var position = _gridOccupation.GetWorldPosition(x, y);
                // TODO: Make positioning cleaner? Not accounting for cell-size right now...

                MeshUtils.AddToMeshArrays(occupationDebugMeshVertices, occupationDebugMeshUv, occupationDebugMeshTriangles, index,
                    position + quadSize * .0f, 0f, quadSize, uv00,
                    uv11);
            }
        }

        _occupationGridMesh.vertices = occupationDebugMeshVertices;
        _occupationGridMesh.uv = occupationDebugMeshUv;
        _occupationGridMesh.triangles = occupationDebugMeshTriangles;
    }
}
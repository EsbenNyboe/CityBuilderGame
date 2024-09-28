using CodeMonkey.Utils;
using UnityEngine;

public class PathfindingVisual : MonoBehaviour
{
    [SerializeField] private MeshFilter _pathGridMeshFilter;
    [SerializeField] private MeshFilter _damageableGridMeshFilter;

    private Grid<GridPath> _gridPath;
    private Grid<GridDamageable> _gridDamageable;

    private Mesh _pathGridMesh;
    private Mesh _damageableGridMesh;

    private bool _updateMesh;
    private bool _updateHealthBar;
    private bool _updateText;

    private TextMesh[,] _debugTextArray;

    private void Awake()
    {
        _pathGridMesh = new Mesh();
        _pathGridMeshFilter.mesh = _pathGridMesh;
        _damageableGridMesh = new Mesh();
        _damageableGridMeshFilter.mesh = _damageableGridMesh;
    }

    private void LateUpdate()
    {
        if (_updateMesh)
        {
            _updateMesh = false;
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
    }

    public void SetGrid(Grid<GridPath> gridPath, Grid<GridDamageable> gridDamageable)
    {
        _gridPath = gridPath;
        _gridDamageable = gridDamageable;
        UpdateVisual();
        UpdateHealthBarVisuals();

        gridPath.OnGridObjectChanged += Grid_OnGridValueChanged;
        gridDamageable.OnGridObjectChanged += Grid_OnGridValueChanged;
    }

    private void Grid_OnGridValueChanged(object sender, Grid<GridDamageable>.OnGridObjectChangedEventArgs e)
    {
        _updateText = true;
        _updateHealthBar = true;
    }

    private void Grid_OnGridValueChanged(object sender, Grid<GridPath>.OnGridObjectChangedEventArgs e)
    {
        _updateMesh = true;
    }

    private void UpdateVisual()
    {
        MeshUtils.CreateEmptyMeshArrays(_gridPath.GetWidth() * _gridPath.GetHeight(), out var vertices, out var uv, out var triangles);

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

                MeshUtils.AddToMeshArrays(vertices, uv, triangles, index, _gridPath.GetWorldPosition(x, y) + quadSize * .0f, 0f, quadSize, uv00,
                    uv11);
            }
        }

        _pathGridMesh.vertices = vertices;
        _pathGridMesh.uv = uv;
        _pathGridMesh.triangles = triangles;
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
        MeshUtils.CreateEmptyMeshArrays(_gridDamageable.GetWidth() * _gridDamageable.GetHeight(), out var vertices, out var uv, out var triangles);

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

                MeshUtils.AddToMeshArrays(vertices, uv, triangles, index, position + quadSize * .0f, 0f, quadSize, uv00,
                    uv11);
            }
        }

        _damageableGridMesh.vertices = vertices;
        _damageableGridMesh.uv = uv;
        _damageableGridMesh.triangles = triangles;
    }
}
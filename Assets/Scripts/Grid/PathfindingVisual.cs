using CodeMonkey.Utils;
using UnityEngine;

public class PathfindingVisual : MonoBehaviour
{
    private Grid<GridPath> _gridPath;
    private Grid<GridDamageable> _gridDamageable;
    private Mesh _mesh;
    private bool _updateMesh;
    private bool _updateText;

    private TextMesh[,] _debugTextArray;

    private void Awake()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
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
    }

    public void SetGrid(Grid<GridPath> gridPath, Grid<GridDamageable> gridDamageable)
    {
        _gridPath = gridPath;
        _gridDamageable = gridDamageable;
        UpdateVisual();

        gridPath.OnGridObjectChanged += Grid_OnGridValueChanged;
        gridDamageable.OnGridObjectChanged += Grid_OnGridValueChanged;
    }

    private void Grid_OnGridValueChanged(object sender, Grid<GridDamageable>.OnGridObjectChangedEventArgs e)
    {
        _updateText = true;
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
                    //quadSize = Vector3.zero;
                    uv00 = new Vector2(.5f, .5f);
                    uv11 = new Vector2(1f, 1f);
                }

                MeshUtils.AddToMeshArrays(vertices, uv, triangles, index, _gridPath.GetWorldPosition(x, y) + quadSize * .0f, 0f, quadSize, uv00,
                    uv11);
            }
        }

        _mesh.vertices = vertices;
        _mesh.uv = uv;
        _mesh.triangles = triangles;
    }

    private void UpdateTextVisual()
    {
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
}
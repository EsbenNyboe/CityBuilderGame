using UnityEngine;

public class PathfindingVisual : MonoBehaviour
{
    private Grid<GridNode> _grid;
    private Mesh _mesh;
    private bool _updateMesh;

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
    }

    public void SetGrid(Grid<GridNode> grid)
    {
        _grid = grid;
        UpdateVisual();

        grid.OnGridObjectChanged += Grid_OnGridValueChanged;
    }

    private void Grid_OnGridValueChanged(object sender, Grid<GridNode>.OnGridObjectChangedEventArgs e)
    {
        _updateMesh = true;
    }

    private void UpdateVisual()
    {
        MeshUtils.CreateEmptyMeshArrays(_grid.GetWidth() * _grid.GetHeight(), out var vertices, out var uv, out var triangles);

        for (var x = 0; x < _grid.GetWidth(); x++)
        {
            for (var y = 0; y < _grid.GetHeight(); y++)
            {
                var index = x * _grid.GetHeight() + y;
                var quadSize = new Vector3(1, 1) * _grid.GetCellSize();

                var gridNode = _grid.GetGridObject(x, y);

                var uv00 = new Vector2(0, 0);
                var uv11 = new Vector2(.5f, .5f);

                if (!gridNode.IsWalkable())
                {
                    //quadSize = Vector3.zero;
                    uv00 = new Vector2(.5f, .5f);
                    uv11 = new Vector2(1f, 1f);
                }

                MeshUtils.AddToMeshArrays(vertices, uv, triangles, index, _grid.GetWorldPosition(x, y) + quadSize * .0f, 0f, quadSize, uv00, uv11);
            }
        }

        _mesh.vertices = vertices;
        _mesh.uv = uv;
        _mesh.triangles = triangles;
    }
}
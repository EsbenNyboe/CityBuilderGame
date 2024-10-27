using UnityEngine;

public abstract class GridVisual
{
    private Mesh _mesh;
    private int[] _triangles;
    private Vector2[] _uvs;
    private Vector3[] _vertices;

    public Mesh CreateMesh()
    {
        _mesh = new Mesh();
        return _mesh;
    }

    public void InitializeMesh(int gridSize)
    {
        MeshUtils.CreateEmptyMeshArrays(gridSize, out _vertices, out _uvs,
            out _triangles);
    }

    public void UpdateVisual(GridManager gridManager)
    {
        var gridWidth = gridManager.Width;
        var gridHeight = gridManager.Height;

        for (var x = 0; x < gridWidth; x++)
        {
            for (var y = 0; y < gridHeight; y++)
            {
                var index = gridManager.GetIndex(x, y);
                var worldPosition = new Vector3(x, y, 0f); // GridManager currently only supports a cellSize of one, and originPosition of zero
                var quadSize = Vector3.one; // GridManager currently only supports a cellSize of one

                if (!TryGetUpdatedCellVisual(gridManager, index, out var uv00, out var uv11, ref quadSize, ref worldPosition))
                {
                    continue;
                }

                MeshUtils.AddToMeshArrays(_vertices, _uvs, _triangles, index, worldPosition + quadSize * .0f, 0,
                    quadSize,
                    uv00, uv11);
            }
        }

        _mesh.vertices = _vertices;
        _mesh.uv = _uvs;
        _mesh.triangles = _triangles;
    }

    protected abstract bool TryGetUpdatedCellVisual(GridManager gridManager, int index, out Vector2 uv00, out Vector2 uv11, ref Vector3 quadSize,
        ref Vector3 worldPosition);
}
using Unity.Mathematics;
using UnityEngine;

namespace Grid.GridVisuals
{
    public abstract class GridVisual
    {
        private MeshFilter[] _meshFilters;
        private Mesh[] _meshes;
        private int[] _triangles;
        private Vector2[] _uvs;
        private Vector3[] _vertices;
        private int _meshWidth;

        private int[] _meshWidths;
        private int[] _meshStartXs;
        private MeshData[] _meshDatas;

        public void CreateMeshFilters(int gridHeight, int gridWidth, GameObject prefab, Transform parent, Material material)
        {
            if (_meshFilters != null)
            {
                for (var i = 0; i < _meshFilters.Length; ++i)
                {
                    Object.Destroy(_meshFilters[i].gameObject);
                }
            }

            const int maxMeshSize = 16000;
            var meshCount = 1;
            while (gridHeight * gridWidth / meshCount > maxMeshSize)
            {
                meshCount++;
            }

            _meshFilters = new MeshFilter[meshCount];
            _meshes = new Mesh[meshCount];
            _meshStartXs = new int[meshCount];
            _meshWidths = new int[meshCount];
            _meshDatas = new MeshData[meshCount];

            var maxMeshWidth = math.min(maxMeshSize / gridHeight, gridWidth);
            var meshWidthSum = 0;
            for (var i = 0; i < meshCount; i++)
            {
                var meshRenderer = Object.Instantiate(prefab).GetComponent<MeshRenderer>();
                meshRenderer.material = material;
                meshRenderer.gameObject.name = "Mesh: " + material.name;
                _meshFilters[i] = meshRenderer.GetComponent<MeshFilter>();
                _meshFilters[i].transform.SetParent(parent);
                _meshFilters[i].mesh = _meshes[i] = new Mesh();
                var meshWidth = maxMeshWidth;
                if (meshWidthSum + meshWidth > gridWidth)
                {
                    meshWidth = gridWidth - meshWidthSum;
                }

                _meshStartXs[i] = meshWidthSum;
                _meshWidths[i] = meshWidth;
                meshWidthSum += meshWidth;
                var meshData = new MeshData();
                MeshUtils.CreateEmptyMeshArrays(gridHeight * meshWidth, out meshData.Vertices, out meshData.Uvs, out meshData.Triangles);
                _meshDatas[i] = meshData;
            }
        }

        public void UpdateVisualNew(GridManager gridManager)
        {
            for (var i = 0; i < _meshes.Length; i++)
            {
                var meshStartX = _meshStartXs[i];
                var meshEndX = meshStartX + _meshWidths[i];
                var gridHeight = gridManager.Height;

                for (var x = meshStartX; x < meshEndX; x++)
                {
                    for (var y = 0; y < gridHeight; y++)
                    {
                        var index = gridManager.GetIndex(x, y);
                        var worldPosition =
                            new Vector3(x, y, 0f); // GridManager currently only supports a cellSize of one, and originPosition of zero
                        var quadSize = Vector3.one; // GridManager currently only supports a cellSize of one

                        if (!TryGetUpdatedCellVisual(gridManager, index, out var uv00, out var uv11, ref quadSize, ref worldPosition))
                        {
                            continue;
                        }

                        MeshUtils.AddToMeshArrays(_meshDatas[i].Vertices, _meshDatas[i].Uvs, _meshDatas[i].Triangles, index - meshStartX * gridHeight,
                            worldPosition + quadSize * .0f, 0,
                            quadSize,
                            uv00, uv11);
                    }
                }

                _meshes[i].vertices = _meshDatas[i].Vertices;
                _meshes[i].uv = _meshDatas[i].Uvs;
                _meshes[i].triangles = _meshDatas[i].Triangles;
            }
        }

        public void CreateMeshContainer(int length)
        {
            _meshes = new Mesh[length];
        }

        public Mesh GetMesh(int index = 0)
        {
            _meshes[index] = new Mesh();
            return _meshes[index];
        }

        public void InitializeMeshData(int gridSize, int slicedWidth = 0)
        {
            _meshWidth = slicedWidth;
            MeshUtils.CreateEmptyMeshArrays(gridSize, out _vertices, out _uvs,
                out _triangles);
        }

        public void UpdateVisual(GridManager gridManager, int meshIndex = 0, int startX = 0)
        {
            var gridWidth = _meshWidth > 0 ? startX + _meshWidth : gridManager.Width;
            var gridHeight = gridManager.Height;

            for (var x = startX; x < gridWidth; x++)
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

                    MeshUtils.AddToMeshArrays(_vertices, _uvs, _triangles, index - startX * gridHeight, worldPosition + quadSize * .0f, 0,
                        quadSize,
                        uv00, uv11);
                }
            }

            _meshes[meshIndex].vertices = _vertices;
            _meshes[meshIndex].uv = _uvs;
            _meshes[meshIndex].triangles = _triangles;
        }

        protected abstract bool TryGetUpdatedCellVisual(GridManager gridManager, int index, out Vector2 uv00, out Vector2 uv11, ref Vector3 quadSize,
            ref Vector3 worldPosition);

        private struct MeshData
        {
            public int[] Triangles;
            public Vector2[] Uvs;
            public Vector3[] Vertices;
        }
    }
}
using Unity.Entities;
using UnityEngine;

public class GridVisuals : MonoBehaviour
{
    [SerializeField] private MeshFilter _pathMeshFilter;
    [SerializeField] private MeshFilter _pathMeshFilterTest;
    [SerializeField] private MeshFilter _treeMeshFilter;
    [SerializeField] private MeshFilter _healthBarMeshFilter;
    [SerializeField] private MeshFilter _occupationDebugMeshFilter;

    private Mesh _pathMesh;
    private Mesh _pathMeshTest;
    private Mesh _treeMesh;
    private Mesh _healthBarMesh;
    private Mesh _occupationDebugMesh;
    private TextMesh[,] _debugTextArray;

    private bool _updatePathMesh;
    private bool _updateTreeMesh;
    private bool _updateDamageableMeshes;
    private bool _updateOccupationDebugMesh;
    private bool _updateDamageableText;

    private int[] pathMeshTriangles;
    private int[] pathMeshTrianglesTest;
    private int[] treeTriangles;
    private int[] healthBarTriangles;
    private int[] occupationDebugTriangles;

    private Vector2[] pathUv;
    private Vector2[] pathUvTest;
    private Vector2[] treeUv;
    private Vector2[] healthBarUv;
    private Vector2[] occupationDebugUv;

    private Vector3[] pathVertices;
    private Vector3[] pathVerticesTest;
    private Vector3[] treeVertices;
    private Vector3[] healthBarVertices;
    private Vector3[] occupationDebugVertices;

    private SystemHandle _gridManagerSystemHandle;

    private void Awake()
    {
        _pathMesh = new Mesh();
        _pathMeshFilter.mesh = _pathMesh;
        _pathMeshTest = new Mesh();
        _pathMeshFilterTest.mesh = _pathMeshTest;
        _treeMesh = new Mesh();
        _treeMeshFilter.mesh = _treeMesh;
        _healthBarMesh = new Mesh();
        _healthBarMeshFilter.mesh = _healthBarMesh;
        _occupationDebugMesh = new Mesh();
        _occupationDebugMeshFilter.mesh = _occupationDebugMesh;
    }

    private void LateUpdate()
    {
        // HACK: This is done in late-update to make sure, the GridManagerSystem has been created. Not sure, if it's necessary though...
        if (_gridManagerSystemHandle == default)
        {
            _gridManagerSystemHandle = World.DefaultGameObjectInjectionWorld.GetExistingSystem<GridManagerSystem>();
            var gridManagerTemp = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<GridManager>(_gridManagerSystemHandle);

            MeshUtils.CreateEmptyMeshArrays(gridManagerTemp.WalkableGrid.Length, out pathVerticesTest, out pathUvTest, out pathMeshTrianglesTest);
            MeshUtils.CreateEmptyMeshArrays(gridManagerTemp.DamageableGrid.Length, out treeVertices, out treeUv, out treeTriangles);
            MeshUtils.CreateEmptyMeshArrays(gridManagerTemp.DamageableGrid.Length, out healthBarVertices, out healthBarUv, out healthBarTriangles);
            MeshUtils.CreateEmptyMeshArrays(gridManagerTemp.OccupiableGrid.Length, out occupationDebugVertices, out occupationDebugUv,
                out occupationDebugTriangles);
        }

        var wasDirty = false;

        var gridManager = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<GridManager>(_gridManagerSystemHandle);
        if (gridManager.WalkableGridIsDirty)
        {
            gridManager.WalkableGridIsDirty = false;
            wasDirty = true;

            UpdateGridManagerVisual(ref gridManager);
        }

        if (gridManager.DamageableGridIsDirty)
        {
            gridManager.DamageableGridIsDirty = false;
            wasDirty = true;
            UpdateGridManagerDamageableVisuals(ref gridManager);
        }

        if (gridManager.OccupiableGridIsDirty)
        {
            gridManager.OccupiableGridIsDirty = false;
            wasDirty = true;
            UpdateGridManagerOccupiableVisuals(ref gridManager);
        }

        if (wasDirty)
        {
            World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(_gridManagerSystemHandle, gridManager);
        }
    }

    private void UpdateGridManagerVisual(ref GridManager gridManager)
    {
        var gridWidth = gridManager.Width;
        var gridHeight = gridManager.Height;

        for (var x = 0; x < gridWidth; x++)
        {
            for (var y = 0; y < gridHeight; y++)
            {
                var index = gridManager.GetIndex(x, y);
                var walkableCell = gridManager.WalkableGrid[index];
                if (!walkableCell.IsDirty)
                {
                    continue;
                }

                walkableCell.IsDirty = false;
                gridManager.WalkableGrid[index] = walkableCell;

                var uv00 = new Vector2(0, 0);
                var uv11 = new Vector2(.5f, .5f);

                if (!walkableCell.IsWalkable)
                {
                    //quadSize = Vector3.zero;
                    uv00 = new Vector2(.5f, .5f);
                    uv11 = new Vector2(1f, 1f);
                }

                var quadSize = Vector3.one; // GridManager currently only supports a cellSize of one
                var worldPosition = new Vector3(x, y, 0f); // GridManager currently only supports a cellSize of one, and originPosition of zero
                MeshUtils.AddToMeshArrays(pathVerticesTest, pathUvTest, pathMeshTrianglesTest, index, worldPosition + quadSize * .0f, 0, quadSize,
                    uv00, uv11);
            }
        }

        _pathMeshTest.vertices = pathVerticesTest;
        _pathMeshTest.uv = pathUvTest;
        _pathMeshTest.triangles = pathMeshTrianglesTest;
    }

    private void UpdateGridManagerDamageableVisuals(ref GridManager gridManager)
    {
        UpdateTreeVisuals(ref gridManager);
        UpdateHealthbarVisuals(ref gridManager);
    }

    private void UpdateTreeVisuals(ref GridManager gridManager)
    {
        var gridWidth = gridManager.Width;
        var gridHeight = gridManager.Height;
        for (var x = 0; x < gridWidth; x++)
        {
            for (var y = 0; y < gridHeight; y++)
            {
                var index = gridManager.GetIndex(x, y);
                var damageableCell = gridManager.DamageableGrid[index];
                if (!damageableCell.IsDirty)
                {
                    continue;
                }

                // Note: The reason we don't call ClearDirty() is, because we need to wait for healthBars to update first
                // damageableCell.IsDirty = false;
                // gridManager.DamageableGrid[index] = damageableCell;

                var health = damageableCell.Health;

                var maxHealth = damageableCell.MaxHealth;
                var healthNormalized = health / maxHealth;

                var frameSize = 0.25f;

                var frameOffset = 0.0f; // max health tree

                if (healthNormalized < 1f)
                {
                    frameOffset = 0.25f;
                }

                if (healthNormalized < 0.66f)
                {
                    frameOffset = 0.5f;
                }

                if (healthNormalized < 0.33f)
                {
                    frameOffset = 0.75f;
                }

                if (health <= 0)
                {
                    // There's no tree
                    frameOffset = 1f;
                }

                var uv00 = new Vector2(frameOffset, 0);
                var uv11 = new Vector2(frameOffset + frameSize, 1);

                var quadSize = Vector3.one; // GridManager currently only supports a cellSize of one
                var worldPosition = new Vector3(x, y, 0f); // GridManager currently only supports a cellSize of one, and originPosition of zero

                MeshUtils.AddToMeshArrays(treeVertices, treeUv, treeTriangles, index, worldPosition + quadSize * .0f, 0, quadSize, uv00,
                    uv11);
            }
        }

        _treeMesh.vertices = treeVertices;
        _treeMesh.uv = treeUv;
        _treeMesh.triangles = treeTriangles;
    }

    private void UpdateHealthbarVisuals(ref GridManager gridManager)
    {
        var gridWidth = gridManager.Width;
        var gridHeight = gridManager.Height;
        for (var x = 0; x < gridWidth; x++)
        {
            for (var y = 0; y < gridHeight; y++)
            {
                var index = gridManager.GetIndex(x, y);
                var damageableCell = gridManager.DamageableGrid[index];
                if (!damageableCell.IsDirty)
                {
                    continue;
                }

                damageableCell.IsDirty = false;
                gridManager.DamageableGrid[index] = damageableCell;

                var healthNormalized = damageableCell.Health / damageableCell.MaxHealth;
                var widthPercentage = 0.75f;
                var quadWidth = healthNormalized * widthPercentage;

                var quadHeight = 0.1f;

                var cellSize = 1f; // GridManager currently only supports a cellSize of one
                var worldPosition = new Vector3(x, y, 0f); // GridManager currently only supports a cellSize of one, and originPosition of zero

                var quadSize = damageableCell.Health > 0
                    ? new Vector3(quadWidth, quadHeight) * cellSize
                    : Vector3.zero;

                var green = 0.9f;
                var yellow = 0.4f;
                var red = 0.1f;
                var color = healthNormalized switch
                {
                    > 0.85f => green,
                    > 0.4f => yellow,
                    _ => red
                };

                var uv00 = new Vector2(color, 0f);
                var uv11 = new Vector2(color, 1f);

                // TODO: Make positioning cleaner? Not accounting for cell-size right now...
                // position.x += widthPercentage * 0.5f;
                worldPosition.y += 0.4f;
                if (healthNormalized < 1)
                {
                    worldPosition.x -= (1 - healthNormalized) / 2;
                }
                else
                {
                    // Hide health, if full
                    quadSize = Vector3.zero;
                }

                MeshUtils.AddToMeshArrays(healthBarVertices, healthBarUv, healthBarTriangles, index, worldPosition + quadSize * .0f, 0, quadSize,
                    uv00,
                    uv11);
            }
        }

        _healthBarMesh.vertices = healthBarVertices;
        _healthBarMesh.uv = healthBarUv;
        _healthBarMesh.triangles = healthBarTriangles;
    }

    private void UpdateGridManagerOccupiableVisuals(ref GridManager gridManager)
    {
        if (!DebugGlobals.ShowOccupationGrid())
        {
            _occupationDebugMesh = new Mesh();
            _occupationDebugMeshFilter.mesh = _occupationDebugMesh;
            return;
        }

        for (var x = 0; x < gridManager.Width; x++)
        {
            for (var y = 0; y < gridManager.Height; y++)
            {
                var index = gridManager.GetIndex(x, y);
                var cellSize = 1f; // GridManager currently only supports a cellSize of one
                var worldPosition = new Vector3(x, y, 0f); // GridManager currently only supports a cellSize of one, and originPosition of zero

                var quadWidth = 0.5f;
                var quadHeight = 0.5f;
                var quadSize = gridManager.IsOccupied(index) ? new Vector3(quadWidth, quadHeight) * cellSize : Vector3.zero;

                var colorRed = 0.1f;
                var uv00 = new Vector2(colorRed, 0f);
                var uv11 = new Vector2(colorRed, 1f);

                MeshUtils.AddToMeshArrays(occupationDebugVertices, occupationDebugUv, occupationDebugTriangles, index,
                    worldPosition + quadSize * .0f, 0, quadSize, uv00,
                    uv11);
            }
        }

        _occupationDebugMesh.vertices = occupationDebugVertices;
        _occupationDebugMesh.uv = occupationDebugUv;
        _occupationDebugMesh.triangles = occupationDebugTriangles;
    }
}
﻿using CodeMonkey.Utils;
using UnityEngine;

public class GridVisuals : MonoBehaviour
{
    [SerializeField] private MeshFilter _pathMeshFilter;
    [SerializeField] private MeshFilter _treeMeshFilter;
    [SerializeField] private MeshFilter _healthBarMeshFilter;
    [SerializeField] private MeshFilter _occupationDebugMeshFilter;

    public float FrameSize;
    public float FrameOffset;

    private Mesh _pathMesh;
    private Mesh _treeMesh;
    private Mesh _healthBarMesh;
    private Mesh _occupationDebugMesh;
    private TextMesh[,] _debugTextArray;

    private Grid<GridPath> _gridPath;
    private Grid<GridDamageable> _gridDamageable;
    private Grid<GridOccupation> _gridOccupation;

    private bool _updatePathMesh;
    private bool _updateTreeMesh;
    private bool _updateDamageableMeshes;
    private bool _updateOccupationDebugMesh;
    private bool _updateDamageableText;

    private int[] pathMeshTriangles;
    private int[] treeTriangles;
    private int[] healthBarTriangles;
    private int[] occupationDebugTriangles;

    private Vector2[] pathUv;
    private Vector2[] treeUv;
    private Vector2[] healthBarUv;
    private Vector2[] occupationDebugUv;

    private Vector3[] pathVertices;
    private Vector3[] treeVertices;
    private Vector3[] healthBarVertices;
    private Vector3[] occupationDebugVertices;

    private void Awake()
    {
        _pathMesh = new Mesh();
        _pathMeshFilter.mesh = _pathMesh;
        _treeMesh = new Mesh();
        _treeMeshFilter.mesh = _treeMesh;
        _healthBarMesh = new Mesh();
        _healthBarMeshFilter.mesh = _healthBarMesh;
        _occupationDebugMesh = new Mesh();
        _occupationDebugMeshFilter.mesh = _occupationDebugMesh;
    }

    private void LateUpdate()
    {
        if (_updatePathMesh)
        {
            _updatePathMesh = false;
            UpdateVisual();
        }

        if (_updateDamageableText)
        {
            _updateDamageableText = false;
            // UpdateTextVisual();
        }

        if (_updateDamageableMeshes)
        {
            _updateDamageableMeshes = false;
            UpdateTreeVisuals();
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


        MeshUtils.CreateEmptyMeshArrays(_gridDamageable.GetWidth() * _gridDamageable.GetHeight(), out pathVertices, out pathUv,
            out pathMeshTriangles);
        MeshUtils.CreateEmptyMeshArrays(_gridDamageable.GetWidth() * _gridDamageable.GetHeight(), out treeVertices, out treeUv,
            out treeTriangles);
        MeshUtils.CreateEmptyMeshArrays(_gridDamageable.GetWidth() * _gridDamageable.GetHeight(), out healthBarVertices, out healthBarUv,
            out healthBarTriangles);
        MeshUtils.CreateEmptyMeshArrays(_gridDamageable.GetWidth() * _gridDamageable.GetHeight(), out occupationDebugVertices,
            out occupationDebugUv, out occupationDebugTriangles);

        UpdateVisual();
        UpdateTreeVisuals();
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
        _updateDamageableText = true;
        _updateDamageableMeshes = true;
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

                uv00 = new Vector2(.5f, .5f);
                uv11 = new Vector2(1f, 1f);

                // if (!gridNode.IsWalkable())
                // {
                //     //quadSize = Vector3.zero;
                //     uv00 = new Vector2(.5f, .5f);
                //     uv11 = new Vector2(1f, 1f);
                // }

                MeshUtils.AddToMeshArrays(pathVertices, pathUv, pathMeshTriangles, index, _gridPath.GetWorldPosition(x, y) + quadSize * .0f,
                    0f, quadSize, uv00,
                    uv11);
            }
        }

        _pathMesh.vertices = pathVertices;
        _pathMesh.uv = pathUv;
        _pathMesh.triangles = pathMeshTriangles;
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

    private void UpdateTreeVisuals()
    {
        var numberOfTrees = 0;

        for (var x = 0; x < _gridDamageable.GetWidth(); x++)
        {
            for (var y = 0; y < _gridDamageable.GetHeight(); y++)
            {
                numberOfTrees++;

                var gridDamageableObject = _gridDamageable.GetGridObject(x, y);
                var health = gridDamageableObject.GetHealth();

                var maxHealth = gridDamageableObject.GetMaxHealth();
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
                    numberOfTrees--;
                }

                var uv00 = new Vector3(frameOffset, 0);
                var uv11 = new Vector3(frameOffset + frameSize, 1);


                var index = x * _gridDamageable.GetHeight() + y;
                var quadSize = new Vector3(1, 1) * _gridPath.GetCellSize();
                var position = _gridDamageable.GetWorldPosition(x, y);

                MeshUtils.AddToMeshArrays(treeVertices, treeUv, treeTriangles, index, position + quadSize * .0f, 0f, quadSize, uv00,
                    uv11);
            }
        }

        _treeMesh.vertices = treeVertices;
        _treeMesh.uv = treeUv;
        _treeMesh.triangles = treeTriangles;

        GlobalStatusDisplay.SetNumberOfTrees(numberOfTrees);
    }

    private void UpdateHealthBarVisuals()
    {
        for (var x = 0; x < _gridDamageable.GetWidth(); x++)
        {
            for (var y = 0; y < _gridDamageable.GetHeight(); y++)
            {
                var index = x * _gridDamageable.GetHeight() + y;
                var gridDamageableObject = _gridDamageable.GetGridObject(x, y);

                var healthNormalized = gridDamageableObject.GetHealth() / gridDamageableObject.GetMaxHealth();
                var widthPercentage = 0.75f;
                var quadWidth = healthNormalized * widthPercentage;

                var quadHeight = 0.1f;
                var quadSize = gridDamageableObject.IsDamageable()
                    ? new Vector3(quadWidth, quadHeight) * _gridDamageable.GetCellSize()
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

                var uv00 = new Vector3(color, 0f);
                var uv11 = new Vector3(color, 1f);

                var position = _gridDamageable.GetWorldPosition(x, y);
                // TODO: Make positioning cleaner? Not accounting for cell-size right now...
                // position.x += widthPercentage * 0.5f;
                position.y += 0.4f;
                if (healthNormalized < 1)
                {
                    position.x -= (1 - healthNormalized) / 2;
                }
                else
                {
                    // Hide health, if full
                    quadSize = Vector3.zero;
                }

                MeshUtils.AddToMeshArrays(healthBarVertices, healthBarUv, healthBarTriangles, index, position + quadSize * .0f, 0f, quadSize, uv00,
                    uv11);
            }
        }

        _healthBarMesh.vertices = healthBarVertices;
        _healthBarMesh.uv = healthBarUv;
        _healthBarMesh.triangles = healthBarTriangles;
    }

    private void UpdateOccupationDebugMesh()
    {
        if (!DebugGlobals.ShowOccupationGrid())
        {
            _occupationDebugMesh = new Mesh();
            _occupationDebugMeshFilter.mesh = _occupationDebugMesh;
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

                MeshUtils.AddToMeshArrays(occupationDebugVertices, occupationDebugUv, occupationDebugTriangles, index,
                    position + quadSize * .0f, 0f, quadSize, uv00,
                    uv11);
            }
        }

        _occupationDebugMesh.vertices = occupationDebugVertices;
        _occupationDebugMesh.uv = occupationDebugUv;
        _occupationDebugMesh.triangles = occupationDebugTriangles;
    }
}
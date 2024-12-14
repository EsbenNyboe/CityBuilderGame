﻿using Unity.Entities;
using UnityEngine;

public class GridVisualsManager : MonoBehaviour
{
    public static GridVisualsManager Instance;

    [SerializeField] private GameObject _pathMeshPrefab;

    [SerializeField] private MeshFilter _pathDebugMeshFilter;
    [SerializeField] private MeshFilter _interactableMeshFilter;
    [SerializeField] private MeshFilter _healthBarMeshFilter;
    [SerializeField] private MeshFilter _occupationDebugMeshFilter;

    private readonly PathGridVisual _pathGridVisual = new();
    private readonly PathGridDebugVisual _pathGridDebugVisual = new();
    private readonly InteractableGridDebugVisual _interactableGridVisual = new();
    private readonly HealthbarGridVisual _healthbarGridVisual = new();
    private readonly OccupationDebugGridVisual _occupationDebugGridVisual = new();

    private bool _hasUpdatedOnce;

    private bool _isInitialized;
    private EntityQuery _gridManagerQuery;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _gridManagerQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<GridManager>());
    }

    private void LateUpdate()
    {
        if (!_gridManagerQuery
                .TryGetSingleton<GridManager>(out var gridManager) ||
            !gridManager.IsInitialized())
        {
            return;
        }

        if (!_isInitialized)
        {
            _isInitialized = true;

            _pathGridVisual.CreateMeshFilters(gridManager.Height, gridManager.Width, _pathMeshPrefab, transform);

            _pathGridDebugVisual.CreateMeshContainer(1);
            _interactableGridVisual.CreateMeshContainer(1);
            _healthbarGridVisual.CreateMeshContainer(1);
            _occupationDebugGridVisual.CreateMeshContainer(1);

            _pathDebugMeshFilter.mesh = _pathGridDebugVisual.GetMesh();
            _interactableMeshFilter.mesh = _interactableGridVisual.GetMesh();
            _healthBarMeshFilter.mesh = _healthbarGridVisual.GetMesh();
            _occupationDebugMeshFilter.mesh = _occupationDebugGridVisual.GetMesh();

            var gridSize = gridManager.WalkableGrid.Length;
            _pathGridDebugVisual.InitializeMeshData(gridSize);
            _interactableGridVisual.InitializeMeshData(gridSize);
            _healthbarGridVisual.InitializeMeshData(gridSize);
            _occupationDebugGridVisual.InitializeMeshData(gridSize);
        }

        var wasDirty = false;

        TryUpdateWalkableGridVisuals(ref gridManager, ref wasDirty);
        TryUpdateDamageableGridVisuals(ref gridManager, ref wasDirty);
        TryUpdateOccupiableGridVisuals(ref gridManager, ref wasDirty);
        TryUpdateInteractableGridVisuals(ref gridManager, ref wasDirty);

        if (wasDirty)
        {
            _gridManagerQuery.SetSingleton(gridManager);
        }

        _hasUpdatedOnce = true;
    }

    public void OnGridSizeChanged()
    {
        _isInitialized = false;
        _hasUpdatedOnce = false;
    }

    private void TryUpdateWalkableGridVisuals(ref GridManager gridManager, ref bool wasDirty)
    {
        var showDebug = DebugGlobals.ShowWalkableGrid();
        _pathDebugMeshFilter.gameObject.SetActive(showDebug);

        if (gridManager.WalkableGridIsDirty)
        {
            gridManager.WalkableGridIsDirty = false;
            wasDirty = true;

            if (!_hasUpdatedOnce)
            {
                // This visual is a static background:
                _pathGridVisual.UpdateVisualNew(gridManager);
            }

            if (showDebug)
            {
                _pathGridDebugVisual.UpdateVisual(gridManager);
            }
        }
    }

    private void TryUpdateDamageableGridVisuals(ref GridManager gridManager, ref bool wasDirty)
    {
        if (gridManager.DamageableGridIsDirty)
        {
            gridManager.DamageableGridIsDirty = false;
            wasDirty = true;

            _healthbarGridVisual.UpdateVisual(gridManager);
        }
    }

    private void TryUpdateOccupiableGridVisuals(ref GridManager gridManager, ref bool wasDirty)
    {
        var showDebug = DebugGlobals.ShowOccupationGrid();
        _occupationDebugMeshFilter.gameObject.SetActive(showDebug);

        if (!showDebug)
        {
            return;
        }

        if (gridManager.OccupiableGridIsDirty)
        {
            gridManager.OccupiableGridIsDirty = false;
            wasDirty = true;
            _occupationDebugGridVisual.UpdateVisual(gridManager);
        }
    }

    private void TryUpdateInteractableGridVisuals(ref GridManager gridManager, ref bool wasDirty)
    {
        if (gridManager.InteractableGridIsDirty)
        {
            gridManager.InteractableGridIsDirty = false;
            wasDirty = true;
            _interactableGridVisual.UpdateVisual(gridManager);
        }
    }
}
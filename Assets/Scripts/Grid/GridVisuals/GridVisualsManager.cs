using Debugging;
using Unity.Entities;
using UnityEngine;

namespace Grid.GridVisuals
{
    public class GridVisualsManager : MonoBehaviour
    {
        public static GridVisualsManager Instance;

        [SerializeField] private GameObject _meshRendererPrefab;

        [SerializeField] private Material _groundMaterial;
        [SerializeField] private Material _healthBarMaterial;
        [SerializeField] private Material _pathDebugMaterial;
        [SerializeField] private Material _interactableDebugMaterial;
        [SerializeField] private Material _occupationDebugMaterial;

        private readonly PathGridVisual _groundVisual = new();
        private readonly PathGridDebugVisual _pathDebugVisual = new();
        private readonly InteractableGridDebugVisual _interactableDebugVisual = new();
        private readonly HealthbarGridVisual _healthBarVisual = new();
        private readonly OccupationDebugGridVisual _occupationDebugVisual = new();

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

                var height = gridManager.Height;
                var width = gridManager.Width;
                _groundVisual.CreateMeshFilters(height, width, _meshRendererPrefab, transform, _groundMaterial);
                _healthBarVisual.CreateMeshFilters(height, width, _meshRendererPrefab, transform, _healthBarMaterial);
                _pathDebugVisual.CreateMeshFilters(height, width, _meshRendererPrefab, transform, _pathDebugMaterial);
                _interactableDebugVisual.CreateMeshFilters(height, width, _meshRendererPrefab, transform, _interactableDebugMaterial);
                _occupationDebugVisual.CreateMeshFilters(height, width, _meshRendererPrefab, transform, _occupationDebugMaterial);
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
            _pathDebugVisual.SetActive(showDebug);

            if (gridManager.WalkableGridIsDirty)
            {
                gridManager.WalkableGridIsDirty = false;
                wasDirty = true;

                if (!_hasUpdatedOnce)
                {
                    // This visual is a static background:
                    _groundVisual.UpdateVisual(gridManager);
                }

                if (showDebug)
                {
                    _pathDebugVisual.UpdateVisual(gridManager);
                }
            }
        }

        private void TryUpdateDamageableGridVisuals(ref GridManager gridManager, ref bool wasDirty)
        {
            if (gridManager.DamageableGridIsDirty)
            {
                gridManager.DamageableGridIsDirty = false;
                wasDirty = true;

                _healthBarVisual.UpdateVisual(gridManager);
            }
        }

        private void TryUpdateOccupiableGridVisuals(ref GridManager gridManager, ref bool wasDirty)
        {
            var showDebug = DebugGlobals.ShowOccupationGrid();
            _occupationDebugVisual.SetActive(showDebug);

            if (!showDebug)
            {
                return;
            }

            if (gridManager.OccupiableGridIsDirty)
            {
                gridManager.OccupiableGridIsDirty = false;
                wasDirty = true;
                _occupationDebugVisual.UpdateVisual(gridManager);
            }
        }

        private void TryUpdateInteractableGridVisuals(ref GridManager gridManager, ref bool wasDirty)
        {
            var showDebug = DebugGlobals.ShowInteractableGrid();
            _interactableDebugVisual.SetActive(showDebug);

            if (gridManager.InteractableGridIsDirty)
            {
                gridManager.InteractableGridIsDirty = false;
                wasDirty = true;
                _interactableDebugVisual.UpdateVisual(gridManager);
            }
        }
    }
}
using Unity.Entities;

namespace Grid.Setup
{
    [UpdateInGroup(typeof(GridSystemGroup))]
    [UpdateAfter(typeof(GridManagerSystem))]
    public partial class TreeSpawnerSystem : SystemBase
    {
        private static bool _shouldSpawnTreesOnMouseDown;
        private static bool _isInitialized;
        private SystemHandle _gridManagerSystemHandle;

        protected override void OnCreate()
        {
            _isInitialized = false;
            _gridManagerSystemHandle = World.GetExistingSystem<GridManagerSystem>();
        }

        protected override void OnUpdate()
        {
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var gridWidth = gridManager.Width;
            var gridHeight = gridManager.Height;

            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            var areasToExclude = TreeGridSetup.AreasToExclude();

            for (var x = 0; x < gridWidth; x++)
            {
                for (var y = 0; y < gridHeight; y++)
                {
                    if (ExcludeCell(areasToExclude, x, y))
                    {
                        continue;
                    }

                    var index = gridManager.GetIndex(x, y);
                    TrySpawnTree(gridManager, index);
                }
            }
        }

        private static bool ExcludeCell(TreeGridSetup.AreaToExclude[] areasToExclude, int x, int y)
        {
            foreach (var areaToExclude in areasToExclude)
            {
                if (x >= areaToExclude.StartCell.x && x <= areaToExclude.EndCell.x &&
                    y >= areaToExclude.StartCell.y && y <= areaToExclude.EndCell.y)
                {
                    return true;
                }
            }

            return false;
        }

        private void TrySpawnTree(GridManager gridManager, int gridIndex)
        {
            if (!gridManager.WalkableGrid[gridIndex].IsWalkable || gridManager.DamageableGrid[gridIndex].Health > 0)
            {
                return;
            }

            SpawnTree(gridManager, gridIndex);
        }

        private void SpawnTree(GridManager gridManager, int gridIndex)
        {
            gridManager.SetIsWalkable(gridIndex, false);
            gridManager.SetHealthToMax(gridIndex);
            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
        }
    }
}
using Grid.GridVisuals;
using GridEntityNS;
using UnitBehaviours.Pathing;
using UnitState;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Grid.Dimensions
{
    public partial class GridDimensionsSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var gridManager = SystemAPI.GetSingleton<GridManager>();

            if (TryUpdateGridDimensions(ref gridManager))
            {
                InvalidatePathingOutsideOfGrid(gridManager);
                DestroyEntitiesOutsideOfGrid(gridManager);
            }
        }

        private void InvalidatePathingOutsideOfGrid(GridManager gridManager)
        {
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (localTransform, pathPositions, pathFollow, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, DynamicBuffer<PathPosition>, RefRO<PathFollow>>().WithEntityAccess())
            {
                if (!pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                if (gridManager.IsPositionInsideGrid(pathPositions[0].Position))
                {
                    continue;
                }

                var cell = GridHelpers.GetXY(localTransform.ValueRO.Position);
                if (gridManager.IsPositionInsideGrid(cell) &&
                    gridManager.TryGetNearbyEmptyCellSemiRandom(cell, out var nearbyEmptyCell))
                {
                    ecb.AddComponent(entity, new Pathfinding
                    {
                        StartPosition = cell,
                        EndPosition = nearbyEmptyCell
                    });
                }
                else
                {
                    ecb.SetComponentEnabled<IsAlive>(entity, false);
                }
            }

            ecb.Playback(EntityManager);
        }

        private void DestroyEntitiesOutsideOfGrid(GridManager gridManager)
        {
            using var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (_, localTransform, entity) in SystemAPI.Query<RefRO<GridEntity>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (!gridManager.IsPositionInsideGrid(localTransform.ValueRO.Position))
                {
                    ecb.DestroyEntity(entity);
                }
            }

            foreach (var (isAlive, localTransform, entity) in SystemAPI.Query<RefRO<IsAlive>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (!gridManager.IsPositionInsideGrid(localTransform.ValueRO.Position))
                {
                    ecb.SetComponentEnabled<IsAlive>(entity, false);
                }
            }

            ecb.Playback(EntityManager);
            SystemAPI.SetSingleton(gridManager);

            // HACK:
            World.GetExistingSystem<IsAliveSystem>().Update(World.Unmanaged);
        }

        public static bool TryUpdateGridDimensions(ref GridManager gridManager)
        {
            var config = GridDimensionsConfig.Instance;
            if (config.Width <= 0 || config.Height <= 0)
            {
                config.Width = GridManagerSystem.DefaultWidth;
                config.Height = GridManagerSystem.DefaultHeight;
                return false;
            }

            if (config.Width == gridManager.Width && config.Height == gridManager.Height)
            {
                return false;
            }

            var oldHeight = gridManager.Height;
            var oldWalkableGrid = gridManager.WalkableGrid;
            var oldDamageableGrid = gridManager.DamageableGrid;
            var oldInteractableGrid = gridManager.InteractableGrid;
            var oldOccupiableGrid = gridManager.OccupiableGrid;
            var oldGridEntityTypeGrid = gridManager.GridEntityTypeGrid;
            var oldGridEntityGrid = gridManager.GridEntityGrid;
            GridManagerSystem.CreateGrids(ref gridManager, config.Width, config.Height);
            ReapplyGridState(ref gridManager, oldHeight, oldWalkableGrid, oldDamageableGrid, oldInteractableGrid, oldOccupiableGrid,
                oldGridEntityTypeGrid, oldGridEntityGrid);

            oldWalkableGrid.Dispose();
            oldDamageableGrid.Dispose();
            oldInteractableGrid.Dispose();
            oldOccupiableGrid.Dispose();
            oldGridEntityTypeGrid.Dispose();
            oldGridEntityGrid.Dispose();
            GridVisualsManager.Instance.OnGridSizeChanged();
            return true;
        }

        private static void ReapplyGridState(ref GridManager gridManager, int oldHeight, NativeArray<WalkableCell> oldWalkableGrid,
            NativeArray<DamageableCell> oldDamageableGrid, NativeArray<InteractableCell> oldInteractableGrid,
            NativeArray<OccupiableCell> oldOccupiableGrid, NativeArray<GridEntityType> oldGridEntityTypeGrid, NativeArray<Entity> oldGridEntityGrid)
        {
            var length = math.min(gridManager.WalkableGrid.Length, oldWalkableGrid.Length);
            for (var i = 0; i < length; i++)
            {
                var cell = GetXY(i, oldHeight);
                if (cell.x >= gridManager.Width || cell.y >= gridManager.Height)
                {
                    continue;
                }

                gridManager.SetIsWalkable(cell, oldWalkableGrid[i].IsWalkable);
                gridManager.SetHealth(cell, oldDamageableGrid[i].Health);
                gridManager.SetInteractableCellType(cell, oldInteractableGrid[i].InteractableCellType);
                gridManager.SetOccupant(cell, oldOccupiableGrid[i].Occupant);
                gridManager.SetGridEntity(cell, oldGridEntityGrid[i], oldGridEntityTypeGrid[i]);
            }
        }

        private static int2 GetXY(int i, int height)
        {
            return new int2(i / height, i % height);
        }
    }
}
using GridEntityNS;
using SystemGroups;
using UnitBehaviours.Pathing;
using UnitState;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Grid
{
    public partial struct GridManager : IComponentData
    {
        public int Width;
        public int Height;
        public NativeArray<WalkableCell> WalkableGrid;
        public NativeArray<DamageableCell> DamageableGrid;
        public NativeArray<OccupiableCell> OccupiableGrid;
        public NativeArray<InteractableCell> InteractableGrid;

        public NativeArray<GridEntityType> GridEntityTypeGrid;
        public NativeArray<Entity> GridEntityGrid;

        public bool WalkableGridIsDirty;
        public bool DamageableGridIsDirty;
        public bool OccupiableGridIsDirty;
        public bool InteractableGridIsDirty;

        // GridSearchHelpers:
        public NativeArray<int2> NeighbourDeltas;
        public int PositionListRadius;
        public NativeArray<int2> PositionList;

        public NativeArray<int2> RelativePositionList;
        public NativeArray<int2> RelativePositionRingInfoList;

        public Random Random;
        public uint RandomSeed;
    }

    [UpdateInGroup(typeof(GridSystemGroup), OrderFirst = true)]
    public partial class GridManagerSystem : SystemBase
    {
        private const int MaxHealth = 100;
        public const int DefaultWidth = 105;
        public const int DefaultHeight = 95;

        protected override void OnCreate()
        {
            var gridManager = new GridManager();
            CreateGrids(ref gridManager, DefaultWidth, DefaultHeight);
            CreateGridSearchHelpers(ref gridManager);

            EntityManager.CreateSingleton<GridManager>();
            SystemAPI.SetSingleton(gridManager);
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            gridManager.RandomSeed++;
            gridManager.Random = Random.CreateFromIndex(gridManager.RandomSeed);
            SystemAPI.SetSingleton(gridManager);

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
                config.Width = DefaultWidth;
                config.Height = DefaultHeight;
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
            CreateGrids(ref gridManager, config.Width, config.Height);
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

        protected override void OnDestroy()
        {
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            DisposeGridData(gridManager);

            // GridSearchHelpers:
            gridManager.NeighbourDeltas.Dispose();
            gridManager.PositionList.Dispose();
            gridManager.RelativePositionList.Dispose();
            gridManager.RelativePositionRingInfoList.Dispose();
        }

        public static void DisposeGridData(GridManager gridManager)
        {
            gridManager.WalkableGrid.Dispose();
            gridManager.DamageableGrid.Dispose();
            gridManager.OccupiableGrid.Dispose();
            gridManager.InteractableGrid.Dispose();
            gridManager.GridEntityGrid.Dispose();
            gridManager.GridEntityTypeGrid.Dispose();
        }

        public static void CreateGrids(ref GridManager gridManager, int width, int height)
        {
            gridManager.Width = width;
            gridManager.Height = height;

            gridManager.WalkableGridIsDirty = true;
            gridManager.WalkableGrid = new NativeArray<WalkableCell>(width * height, Allocator.Persistent);
            for (var i = 0; i < gridManager.WalkableGrid.Length; i++)
            {
                var cell = gridManager.WalkableGrid[i];
                cell.IsWalkable = true;
                cell.IsDirty = true;
                cell.Section = -1;
                gridManager.WalkableGrid[i] = cell;
            }

            gridManager.DamageableGridIsDirty = true;
            gridManager.DamageableGrid = new NativeArray<DamageableCell>(width * height, Allocator.Persistent);
            for (var i = 0; i < gridManager.DamageableGrid.Length; i++)
            {
                var cell = gridManager.DamageableGrid[i];
                cell.Health = 0;
                cell.MaxHealth = MaxHealth;
                cell.IsDirty = true;
                gridManager.DamageableGrid[i] = cell;
            }

            gridManager.OccupiableGridIsDirty = true;
            gridManager.OccupiableGrid = new NativeArray<OccupiableCell>(width * height, Allocator.Persistent);
            for (var i = 0; i < gridManager.OccupiableGrid.Length; i++)
            {
                var cell = gridManager.OccupiableGrid[i];
                cell.Occupant = Entity.Null;
                cell.IsDirty = true;
                gridManager.OccupiableGrid[i] = cell;
            }

            gridManager.InteractableGridIsDirty = true;
            gridManager.InteractableGrid = new NativeArray<InteractableCell>(width * height, Allocator.Persistent);
            for (var i = 0; i < gridManager.InteractableGrid.Length; i++)
            {
                var cell = gridManager.InteractableGrid[i];
                cell.InteractableCellType = InteractableCellType.None;
                cell.IsDirty = true;
                gridManager.InteractableGrid[i] = cell;
            }

            gridManager.GridEntityGrid = new NativeArray<Entity>(width * height, Allocator.Persistent);
            gridManager.GridEntityTypeGrid = new NativeArray<GridEntityType>(width * height, Allocator.Persistent);
        }

        private static void CreateGridSearchHelpers(ref GridManager gridManager)
        {
            gridManager.NeighbourDeltas =
                new NativeArray<int2>(
                    new int2[]
                    {
                        new(1, 0), new(1, 1), new(0, 1), new(-1, 1), new(-1, 0), new(-1, -1), new(0, -1), new(1, -1)
                    },
                    Allocator.Persistent);
            gridManager.PositionListRadius = 50;
            gridManager.PositionList =
                new NativeArray<int2>(GridHelpers.CalculatePositionListLength(gridManager.PositionListRadius),
                    Allocator.Persistent);

            var relativePositionListRadius = 50;
            gridManager.RelativePositionList =
                new NativeArray<int2>(GridHelpers.CalculatePositionListLength(relativePositionListRadius),
                    Allocator.Persistent);
            gridManager.RelativePositionRingInfoList =
                new NativeArray<int2>(relativePositionListRadius, Allocator.Persistent);
            gridManager.PopulateRelativePositionList(relativePositionListRadius);
        }
    }
}
using CodeMonkey.Utils;
using Debugging;
using Grid;
using GridEntityNS;
using Inventory;
using SystemGroups;
using UnitBehaviours.AutonomousHarvesting;
using UnitBehaviours.UnitConfigurators;
using UnitState.SocialState;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.Targeting.Core
{
    public struct QuadrantDataManager : IComponentData
    {
        public NativeParallelMultiHashMap<int, QuadrantData> VillagerQuadrantMap;
        public NativeParallelMultiHashMap<int, QuadrantData> BoarQuadrantMap;
        public NativeParallelMultiHashMap<int, QuadrantData> DroppedItemQuadrantMap;
        public NativeParallelMultiHashMap<int, QuadrantData> DropPointQuadrantMap;
        public NativeParallelMultiHashMap<int, QuadrantData> ConstructableQuadrantMap;
    }

    public struct QuadrantData
    {
        public Entity Entity;
        public float3 Position;
        public int Section;

        public bool IsValid()
        {
            return Entity != Entity.Null;
        }
    }

    [UpdateInGroup(typeof(LifetimeSystemGroup))]
    public partial struct QuadrantSystem : ISystem
    {
        private EntityQuery _villagerQuery;
        private EntityQuery _boarQuery;
        private EntityQuery _droppedItemQuery;
        private EntityQuery _dropPointQuery;
        private EntityQuery _constructableQuery;
        public const int QuadrantYMultiplier = 1000;
        public const int QuadrantCellSize = 10;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<QuadrantDataManager>();
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<DebugToggleManager>();
            _villagerQuery = state.GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<QuadrantEntity>(),
                ComponentType.ReadOnly<Villager>());
            _boarQuery = state.GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<QuadrantEntity>(),
                ComponentType.ReadOnly<Boar>());
            _droppedItemQuery = state.GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(),
                // ComponentType.ReadOnly<QuadrantEntity>(),
                ComponentType.ReadOnly<DroppedItem>());
            var dropPointQueryBuilder = new EntityQueryBuilder(Allocator.Temp).WithAll<LocalTransform, QuadrantEntity, DropPoint>()
                .WithNone<Constructable>();
            _dropPointQuery = state.GetEntityQuery(dropPointQueryBuilder);
            _constructableQuery = state.GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<QuadrantEntity>(),
                ComponentType.ReadOnly<Constructable>());

            state.EntityManager.AddComponent<QuadrantDataManager>(state.SystemHandle);
            SystemAPI.SetComponent(state.SystemHandle, new QuadrantDataManager
            {
                VillagerQuadrantMap = new NativeParallelMultiHashMap<int, QuadrantData>(
                    _villagerQuery.CalculateEntityCount(),
                    Allocator.Persistent),
                BoarQuadrantMap = new NativeParallelMultiHashMap<int, QuadrantData>(
                    _boarQuery.CalculateEntityCount(),
                    Allocator.Persistent),
                DroppedItemQuadrantMap = new NativeParallelMultiHashMap<int, QuadrantData>(
                    _droppedItemQuery.CalculateEntityCount(),
                    Allocator.Persistent),
                DropPointQuadrantMap = new NativeParallelMultiHashMap<int, QuadrantData>(
                    _dropPointQuery.CalculateEntityCount(),
                    Allocator.Persistent),
                ConstructableQuadrantMap = new NativeParallelMultiHashMap<int, QuadrantData>(
                    _constructableQuery.CalculateEntityCount(),
                    Allocator.Persistent)
            });
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            var quadrantDataManager = SystemAPI.GetComponent<QuadrantDataManager>(state.SystemHandle);
            quadrantDataManager.VillagerQuadrantMap.Dispose();
            quadrantDataManager.BoarQuadrantMap.Dispose();
            quadrantDataManager.DroppedItemQuadrantMap.Dispose();
            quadrantDataManager.DropPointQuadrantMap.Dispose();
            quadrantDataManager.ConstructableQuadrantMap.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var isDebugging = SystemAPI.GetSingleton<DebugToggleManager>().DebugQuadrantSystem;
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var quadrantDataManager = SystemAPI.GetSingleton<QuadrantDataManager>();

            if (isDebugging)
            {
                DebugHelper.Log(GetEntityCountInHashmap(quadrantDataManager.VillagerQuadrantMap,
                    GetHashMapKeyFromPosition(UtilsClass.GetMouseWorldPosition())));
            }

            BuildQuadrantMap(ref state, gridManager, _villagerQuery, quadrantDataManager.VillagerQuadrantMap);
            BuildQuadrantMap(ref state, gridManager, _boarQuery, quadrantDataManager.BoarQuadrantMap);
            BuildQuadrantMap(ref state, gridManager, _droppedItemQuery, quadrantDataManager.DroppedItemQuadrantMap);
            BuildQuadrantMap(ref state, gridManager, _dropPointQuery, quadrantDataManager.DropPointQuadrantMap, true);
            BuildQuadrantMap(ref state, gridManager, _constructableQuery, quadrantDataManager.ConstructableQuadrantMap, true);
        }

        [BurstCompile]
        private void BuildQuadrantMap(ref SystemState state,
            GridManager gridManager,
            EntityQuery entityQuery,
            NativeParallelMultiHashMap<int, QuadrantData> quadrantMultiHashMap,
            bool needsAdjacentAcces = false)
        {
            quadrantMultiHashMap.Clear();
            if (entityQuery.CalculateEntityCount() > quadrantMultiHashMap.Capacity)
            {
                quadrantMultiHashMap.Capacity = entityQuery.CalculateEntityCount();
            }

            state.Dependency = new SetQuadrantDataHashMapJob
            {
                QuadrantMultiHashMap = quadrantMultiHashMap.AsParallelWriter(),
                GridManager = gridManager,
                NeedsAdjacentAccess = needsAdjacentAcces
            }.ScheduleParallel(entityQuery, state.Dependency);
        }

        public static int GetHashMapKeyFromPosition(float3 position)
        {
            return (int)(math.floor(position.x / QuadrantCellSize) +
                         QuadrantYMultiplier *
                         math.floor(position.y / QuadrantCellSize));
        }

        public static float3 GetQuadrantCenterPositionFromHashMapKey(int hashMapKey)
        {
            var position = new float3();
            position.x = hashMapKey * QuadrantCellSize % QuadrantYMultiplier + QuadrantCellSize * 0.5f;
            position.y = (float)(hashMapKey * QuadrantCellSize) / QuadrantYMultiplier + QuadrantCellSize * 0.5f;
            return position;
        }

        private static int GetEntityCountInHashmap(NativeParallelMultiHashMap<int, QuadrantData> quadrantMultiHashMap,
            int hashMapKey)
        {
            QuadrantData quadrantData;
            NativeParallelMultiHashMapIterator<int> nativeParallelMultiHashMapIterator;
            var count = 0;
            if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData,
                    out nativeParallelMultiHashMapIterator))
            {
                do
                {
                    count++;
                } while (quadrantMultiHashMap.TryGetNextValue(out quadrantData,
                             ref nativeParallelMultiHashMapIterator));
            }

            return count;
        }

        [BurstCompile]
        private partial struct SetQuadrantDataHashMapJob : IJobEntity
        {
            public NativeParallelMultiHashMap<int, QuadrantData>.ParallelWriter QuadrantMultiHashMap;
            [ReadOnly] public GridManager GridManager;
            [ReadOnly] public bool NeedsAdjacentAccess;

            public void Execute(in Entity entity, in LocalTransform localTransform)
            {
                var position = localTransform.Position;
                var gridIndex = GridManager.GetIndex(position);
                var hashMapKey = GetHashMapKeyFromPosition(position);

                var section = NeedsAdjacentAccess
                    ? GridManager.GetSectionOfNeighbour(GridHelpers.GetXY(position))
                    : GridManager.IsWalkable(gridIndex)
                        ? GridManager.WalkableGrid[gridIndex].Section
                        : -1;

                if (section > -1)
                {
                    QuadrantMultiHashMap.Add(hashMapKey, new QuadrantData
                    {
                        Entity = entity,
                        Position = position,
                        Section = section
                    });
                }
            }
        }

        public static bool TryFindClosestFriend(SocialRelationships socialRelationships,
            NativeParallelMultiHashMap<int, QuadrantData> nmhm,
            GridManager gridManager, int quadrantsToSearch, float3 position, Entity entity,
            out Entity closestTargetEntity, out float closestTargetDistance)
        {
            var relationships = socialRelationships.Relationships;
            PrepareSearch(gridManager, position, out var section, out var key, out closestTargetDistance, out var closestTarget);
            for (var i = 0; i < quadrantsToSearch; i++)
            {
                if (TryPrepareIterator(gridManager, nmhm, i, key, out var quadrantData, out var nmhmIterator))
                {
                    do
                    {
                        if (TryGetClosestDistance(position, quadrantData, closestTargetDistance, section, out var distance) &&
                            !IsSameEntity(entity, quadrantData) &&
                            IsFriend(relationships, quadrantData))
                        {
                            closestTargetDistance = distance;
                            closestTarget = quadrantData;
                        }
                    } while (nmhm.TryGetNextValue(out quadrantData, ref nmhmIterator));
                }
            }

            closestTargetEntity = closestTarget.Entity;
            return closestTarget.IsValid();
        }

        private static bool IsFriend(NativeParallelHashMap<Entity, float> relationships, QuadrantData quadrantData)
        {
            const float friendThreshold = 1f;
            return relationships[quadrantData.Entity] > friendThreshold;
        }

        public static bool TryFindSpaciousStorageInSection(NativeParallelMultiHashMap<int, QuadrantData> quadrantMultiHashMap,
            GridManager gridManager, int quadrantsToSearch, float3 position)
        {
            var section = gridManager.GetSection(position);
            var hashMapKey = GetHashMapKeyFromPosition(position);
            var quadrantIndex = 0;
            while (quadrantIndex < quadrantsToSearch)
            {
                var relativeCoordinate = gridManager.RelativePositionList[quadrantIndex];
                var relativeHashMapKey = relativeCoordinate.x + relativeCoordinate.y * QuadrantYMultiplier;
                var absoluteHashMapKey = hashMapKey + relativeHashMapKey;

                if (quadrantMultiHashMap.TryGetFirstValue(absoluteHashMapKey, out var quadrantData,
                        out var nativeParallelMultiHashMapIterator))
                {
                    do
                    {
                        if (quadrantData.Section == section &&
                            gridManager.GetStorageItemCount(quadrantData.Position) < gridManager.GetStorageItemCapacity(quadrantData.Position))
                        {
                            return true;
                        }
                    } while (quadrantMultiHashMap.TryGetNextValue(out quadrantData,
                                 ref nativeParallelMultiHashMapIterator));
                }

                quadrantIndex++;
            }

            return false;
        }

        public static bool TryFindClosestSpaciousStorage(NativeParallelMultiHashMap<int, QuadrantData> nmhm,
            GridManager gridManager, int quadrantsToSearch, float3 position, out QuadrantData closestTarget)
        {
            PrepareSearch(gridManager, position, out var section, out var key, out var closestTargetDistance, out closestTarget);
            for (var i = 0; i < quadrantsToSearch; i++)
            {
                if (TryPrepareIterator(gridManager, nmhm, i, key, out var quadrantData, out var nmhmIterator))
                {
                    do
                    {
                        if (TryGetClosestDistance(position, quadrantData, closestTargetDistance, section, out var distance) &&
                            IsSpaciousStorage(gridManager, quadrantData))
                        {
                            closestTargetDistance = distance;
                            closestTarget = quadrantData;
                        }
                    } while (nmhm.TryGetNextValue(out quadrantData, ref nmhmIterator));
                }
            }

            return closestTarget.IsValid();
        }

        public static bool TryFindClosestEntity(NativeParallelMultiHashMap<int, QuadrantData> nmhm, GridManager gridManager,
            int quadrantsToSearch, float3 position, Entity entity,
            out Entity closestTargetEntity, out float closestTargetDistance)
        {
            PrepareSearch(gridManager, position, out var section, out var key, out closestTargetDistance, out var closestTarget);

            for (var i = 0; i < quadrantsToSearch; i++)
            {
                if (TryPrepareIterator(gridManager, nmhm, i, key, out var quadrantData, out var nmhmIterator))
                {
                    do
                    {
                        if (TryGetClosestDistance(position, quadrantData, closestTargetDistance, section, out var distance) &&
                            !IsSameEntity(entity, quadrantData))
                        {
                            closestTargetDistance = distance;
                            closestTarget = quadrantData;
                        }
                    } while (nmhm.TryGetNextValue(out quadrantData, ref nmhmIterator));
                }
            }

            closestTargetEntity = closestTarget.Entity;
            return closestTarget.IsValid();
        }

        private static bool IsSameEntity(Entity entity, QuadrantData quadrantData)
        {
            return entity == quadrantData.Entity;
        }

        private static bool TryGetClosestDistance(float3 position, QuadrantData quadrantData, float closestTargetDistance, int section,
            out float distance)
        {
            distance = math.distance(position, quadrantData.Position);
            return distance < closestTargetDistance && quadrantData.Section == section;
        }

        private static void PrepareSearch(GridManager gridManager, float3 position, out int section, out int hashMapKey,
            out float closestTargetDistance, out QuadrantData closestTarget)
        {
            section = gridManager.GetSection(position);
            hashMapKey = GetHashMapKeyFromPosition(position);
            closestTargetDistance = float.MaxValue;
            closestTarget = new QuadrantData();
        }

        private static bool TryPrepareIterator(GridManager gridManager, NativeParallelMultiHashMap<int, QuadrantData> quadrantMultiHashMap,
            int quadrantIndex, int hashMapKey,
            out QuadrantData quadrantData, out NativeParallelMultiHashMapIterator<int> nativeParallelMultiHashMapIterator)
        {
            var relativeCoordinate = gridManager.RelativePositionList[quadrantIndex];
            var relativeHashMapKey = relativeCoordinate.x + relativeCoordinate.y * QuadrantYMultiplier;
            var absoluteHashMapKey = hashMapKey + relativeHashMapKey;

            var hasValue = quadrantMultiHashMap.TryGetFirstValue(absoluteHashMapKey, out quadrantData,
                out nativeParallelMultiHashMapIterator);
            return hasValue;
        }

        private static bool IsSpaciousStorage(GridManager gridManager, QuadrantData quadrantData)
        {
            return gridManager.GetStorageItemCount(quadrantData.Position) < gridManager.GetStorageItemCapacity(quadrantData.Position);
        }
    }
}
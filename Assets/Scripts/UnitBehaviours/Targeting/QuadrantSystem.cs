using CodeMonkey.Utils;
using Debugging;
using UnitState;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.Targeting
{
    public struct QuadrantEntity : IComponentData
    {
    }

    public struct QuadrantDataManager : IComponentData
    {
        public NativeParallelMultiHashMap<int, QuadrantData> VillagerQuadrantMap;
    }

    public struct QuadrantData
    {
        public Entity Entity;
        public float3 Position;
        public int Section;
    }

    [UpdateInGroup(typeof(LifetimeSystemGroup))]
    public partial struct QuadrantSystem : ISystem
    {
        private EntityQuery _villagerQuery;
        public const int QuadrantYMultiplier = 1000;
        public const int QuadrantCellSize = 10;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<QuadrantDataManager>();
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<DebugToggleManager>();
            _villagerQuery = state.GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<QuadrantEntity>(), ComponentType.ReadOnly<Villager>());

            state.EntityManager.AddComponent<QuadrantDataManager>(state.SystemHandle);
            SystemAPI.SetComponent(state.SystemHandle, new QuadrantDataManager
            {
                VillagerQuadrantMap = new NativeParallelMultiHashMap<int, QuadrantData>(
                    _villagerQuery.CalculateEntityCount(),
                    Allocator.Persistent)
            });
        }

        public void OnDestroy(ref SystemState state)
        {
            var quadrantDataManager = SystemAPI.GetComponent<QuadrantDataManager>(state.SystemHandle);
            quadrantDataManager.VillagerQuadrantMap.Dispose();
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
        }

        private void BuildQuadrantMap(ref SystemState state,
            GridManager gridManager,
            EntityQuery entityQuery,
            NativeParallelMultiHashMap<int, QuadrantData> quadrantMultiHashMap)
        {
            quadrantMultiHashMap.Clear();
            if (entityQuery.CalculateEntityCount() > quadrantMultiHashMap.Capacity)
            {
                quadrantMultiHashMap.Capacity = entityQuery.CalculateEntityCount();
            }

            state.Dependency = new SetQuadrantDataHashMapJob
            {
                QuadrantMultiHashMap = quadrantMultiHashMap.AsParallelWriter(),
                GridManager = gridManager
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

            public void Execute(in Entity entity, in LocalTransform localTransform)
            {
                var position = localTransform.Position;
                var gridIndex = GridManager.GetIndex(position);
                var hashMapKey = GetHashMapKeyFromPosition(position);
                QuadrantMultiHashMap.Add(hashMapKey, new QuadrantData
                {
                    Entity = entity,
                    Position = position,
                    Section = GridManager.WalkableGrid[gridIndex].Section
                });
            }
        }

        public static bool TryFindClosestFriend(SocialRelationships socialRelationships,
            NativeParallelMultiHashMap<int, QuadrantData> quadrantMultiHashMap,
            int hashMapKey, int section, float3 position, Entity entity,
            out Entity closestTargetEntity, out float closestTargetDistance)
        {
            closestTargetEntity = Entity.Null;
            closestTargetDistance = float.MaxValue;
            // First check center
            FindFriend(socialRelationships, quadrantMultiHashMap, hashMapKey, section, position,
                ref closestTargetEntity,
                ref closestTargetDistance, entity);

            if (closestTargetEntity != Entity.Null)
            {
                // No need to search neighbours
                return true;
            }

            // Then search neighbours
            FindFriend(socialRelationships, quadrantMultiHashMap, hashMapKey + 1, section, position,
                ref closestTargetEntity,
                ref closestTargetDistance, entity);
            FindFriend(socialRelationships, quadrantMultiHashMap, hashMapKey - 1, section, position,
                ref closestTargetEntity,
                ref closestTargetDistance, entity);
            FindFriend(socialRelationships, quadrantMultiHashMap, hashMapKey + QuadrantYMultiplier, section,
                position, ref closestTargetEntity,
                ref closestTargetDistance, entity);
            FindFriend(socialRelationships, quadrantMultiHashMap, hashMapKey - QuadrantYMultiplier, section,
                position, ref closestTargetEntity,
                ref closestTargetDistance, entity);
            // Then search corners
            FindFriend(socialRelationships, quadrantMultiHashMap, hashMapKey + 1 + QuadrantYMultiplier, section,
                position, ref closestTargetEntity,
                ref closestTargetDistance, entity);
            FindFriend(socialRelationships, quadrantMultiHashMap, hashMapKey - 1 + QuadrantYMultiplier, section,
                position, ref closestTargetEntity,
                ref closestTargetDistance, entity);
            FindFriend(socialRelationships, quadrantMultiHashMap, hashMapKey + 1 - QuadrantYMultiplier, section,
                position, ref closestTargetEntity,
                ref closestTargetDistance, entity);
            FindFriend(socialRelationships, quadrantMultiHashMap, hashMapKey - 1 - QuadrantYMultiplier, section,
                position, ref closestTargetEntity,
                ref closestTargetDistance, entity);

            return closestTargetEntity != Entity.Null;
        }

        public static bool TryFindClosestEntity(NativeParallelMultiHashMap<int, QuadrantData> quadrantMultiHashMap, GridManager gridManager,
            int quadrantsToSearch, float3 position, Entity entity,
            out Entity closestTargetEntity, out float closestTargetDistance)
        {
            closestTargetEntity = Entity.Null;
            closestTargetDistance = float.MaxValue;
            var section = gridManager.GetSection(position);
            var hashMapKey = GetHashMapKeyFromPosition(position);

            var quadrantIndex = 0;
            while (quadrantIndex < quadrantsToSearch)
            {
                var relativeCoordinate = gridManager.RelativePositionList[quadrantIndex];
                var relativeHashMapKey = relativeCoordinate.x + relativeCoordinate.y * QuadrantYMultiplier;
                var absoluteHashMapKey = hashMapKey + relativeHashMapKey;
                FindTarget(quadrantMultiHashMap, absoluteHashMapKey, section, position, ref closestTargetEntity,
                    ref closestTargetDistance, entity);
                quadrantIndex++;
            }

            return closestTargetEntity != Entity.Null;
        }

        private static void FindFriend(SocialRelationships socialRelationships,
            NativeParallelMultiHashMap<int, QuadrantData> quadrantMultiHashMap,
            int hashMapKey, int section,
            float3 position, ref Entity closestTargetEntity,
            ref float closestTargetDistance, Entity entity)
        {
            var relationships = socialRelationships.Relationships;
            const float friendThreshold = 1f;

            if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out var quadrantData,
                    out var nativeParallelMultiHashMapIterator))
            {
                do
                {
                    var distance = math.distance(position, quadrantData.Position);
                    if (distance < closestTargetDistance &&
                        relationships[quadrantData.Entity] > friendThreshold)
                    {
                        // Make sure I'm not targeting myself.
                        // And that my target and I are in the same walkable section.
                        if (entity != quadrantData.Entity && section == quadrantData.Section)
                        {
                            closestTargetDistance = distance;
                            closestTargetEntity = quadrantData.Entity;
                        }
                    }
                } while (quadrantMultiHashMap.TryGetNextValue(out quadrantData,
                             ref nativeParallelMultiHashMapIterator));
            }
        }

        private static void FindTarget(NativeParallelMultiHashMap<int, QuadrantData> quadrantMultiHashMap,
            int hashMapKey, int section,
            float3 position, ref Entity closestTargetEntity,
            ref float closestTargetDistance, Entity entity)
        {
            if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out var quadrantData,
                    out var nativeParallelMultiHashMapIterator))
            {
                do
                {
                    var distance = math.distance(position, quadrantData.Position);
                    if (distance < closestTargetDistance)
                    {
                        // Make sure I'm not targeting myself.
                        // And that my target and I are in the same walkable section.
                        if (entity != quadrantData.Entity && section == quadrantData.Section)
                        {
                            closestTargetDistance = distance;
                            closestTargetEntity = quadrantData.Entity;
                        }
                    }
                } while (quadrantMultiHashMap.TryGetNextValue(out quadrantData,
                             ref nativeParallelMultiHashMapIterator));
            }
        }
    }
}
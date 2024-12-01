using CodeMonkey.Utils;
using Debugging;
using UnitState;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitBehaviours.Targeting
{
    public struct QuadrantEntity : IComponentData
    {
    }

    public struct QuadrantDataManager : IComponentData
    {
        public NativeParallelMultiHashMap<int, QuadrantData> QuadrantMultiHashMap;
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
        private EntityQuery _entityQuery;
        public const int QuadrantYMultiplier = 1000;
        public const int QuadrantCellSize = 10;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<DebugToggleManager>();
            _entityQuery = state.GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<QuadrantEntity>());

            state.EntityManager.AddComponent<QuadrantDataManager>(state.SystemHandle);
            SystemAPI.SetComponent(state.SystemHandle, new QuadrantDataManager
            {
                QuadrantMultiHashMap = new NativeParallelMultiHashMap<int, QuadrantData>(
                    _entityQuery.CalculateEntityCount(),
                    Allocator.Persistent)
            });
        }

        public void OnDestroy(ref SystemState state)
        {
            var quadrantDataManager = SystemAPI.GetComponent<QuadrantDataManager>(state.SystemHandle);
            quadrantDataManager.QuadrantMultiHashMap.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var isDebugging = SystemAPI.GetSingleton<DebugToggleManager>().DebugQuadrantSystem;

            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var quadrantMultiHashMap =
                SystemAPI.GetComponent<QuadrantDataManager>(state.SystemHandle).QuadrantMultiHashMap;

            if (isDebugging)
            {
                DebugDrawQuadrant(UtilsClass.GetMouseWorldPosition());
                DebugHelper.Log(GetEntityCountInHashmap(quadrantMultiHashMap,
                    GetPositionHashMapKey(UtilsClass.GetMouseWorldPosition())));
            }

            quadrantMultiHashMap.Clear();
            if (_entityQuery.CalculateEntityCount() > quadrantMultiHashMap.Capacity)
            {
                quadrantMultiHashMap.Capacity = _entityQuery.CalculateEntityCount();
            }

            state.Dependency = new SetQuadrantDataHashMapJob
            {
                QuadrantMultiHashMap = quadrantMultiHashMap.AsParallelWriter(),
                GridManager = gridManager
            }.ScheduleParallel(_entityQuery, state.Dependency);
        }

        private static void DebugDrawQuadrant(float3 position)
        {
            var lowerLeft = new Vector3(math.floor(position.x / QuadrantCellSize) * QuadrantCellSize,
                math.floor(position.y / QuadrantCellSize) * QuadrantCellSize, 0);
            Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(+1, +0) * QuadrantCellSize);
            Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(+0, +1) * QuadrantCellSize);
            Debug.DrawLine(lowerLeft + new Vector3(+1, +0) * QuadrantCellSize,
                lowerLeft + new Vector3(+1, +1) * QuadrantCellSize);
            Debug.DrawLine(lowerLeft + new Vector3(+0, +1) * QuadrantCellSize,
                lowerLeft + new Vector3(+1, +1) * QuadrantCellSize);
            // Debug.Log(GetPositionHashMapKey(position) + " " + position);
        }

        public static int GetPositionHashMapKey(float3 position)
        {
            return (int)(math.floor(position.x / QuadrantCellSize) +
                         QuadrantYMultiplier *
                         math.floor(position.y / QuadrantCellSize));
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
                var hashMapKey = GetPositionHashMapKey(position);
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

        public static bool TryFindClosestEntity(NativeParallelMultiHashMap<int, QuadrantData> quadrantMultiHashMap,
            int hashMapKey, int section, float3 position, Entity entity,
            out Entity closestTargetEntity, out float closestTargetDistance)
        {
            closestTargetEntity = Entity.Null;
            closestTargetDistance = float.MaxValue;
            // First check center
            FindTarget(quadrantMultiHashMap, hashMapKey, section, position, ref closestTargetEntity,
                ref closestTargetDistance, entity);

            if (closestTargetEntity != Entity.Null)
            {
                // No need to search neighbours
                return true;
            }

            // Then search neighbours
            FindTarget(quadrantMultiHashMap, hashMapKey + 1, section, position, ref closestTargetEntity,
                ref closestTargetDistance, entity);
            FindTarget(quadrantMultiHashMap, hashMapKey - 1, section, position, ref closestTargetEntity,
                ref closestTargetDistance, entity);
            FindTarget(quadrantMultiHashMap, hashMapKey + QuadrantYMultiplier, section,
                position, ref closestTargetEntity,
                ref closestTargetDistance, entity);
            FindTarget(quadrantMultiHashMap, hashMapKey - QuadrantYMultiplier, section,
                position, ref closestTargetEntity,
                ref closestTargetDistance, entity);
            // Then search corners
            FindTarget(quadrantMultiHashMap, hashMapKey + 1 + QuadrantYMultiplier, section,
                position, ref closestTargetEntity,
                ref closestTargetDistance, entity);
            FindTarget(quadrantMultiHashMap, hashMapKey - 1 + QuadrantYMultiplier, section,
                position, ref closestTargetEntity,
                ref closestTargetDistance, entity);
            FindTarget(quadrantMultiHashMap, hashMapKey + 1 - QuadrantYMultiplier, section,
                position, ref closestTargetEntity,
                ref closestTargetDistance, entity);
            FindTarget(quadrantMultiHashMap, hashMapKey - 1 - QuadrantYMultiplier, section,
                position, ref closestTargetEntity,
                ref closestTargetDistance, entity);

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
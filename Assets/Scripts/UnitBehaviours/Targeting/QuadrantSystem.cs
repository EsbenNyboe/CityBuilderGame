using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitBehaviours.Targeting
{
    public struct QuadrantData
    {
        public Entity Entity;
        public float3 Position;
    }

    public struct QuadrantDataManager : IComponentData
    {
        public NativeParallelMultiHashMap<int, QuadrantData> QuadrantMultiHashMap;
    }

    [UpdateInGroup(typeof(LifetimeSystemGroup))]
    public partial struct QuadrantSystem : ISystem
    {
        private EntityQuery _entityQuery;
        public const int QuadrantYMultiplier = 1000;
        public const int QuadrantCellSize = 5;

        public void OnCreate(ref SystemState state)
        {
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

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var quadrantMultiHashMap =
                SystemAPI.GetComponent<QuadrantDataManager>(state.SystemHandle).QuadrantMultiHashMap;

            // DebugDrawQuadrant(UtilsClass.GetMouseWorldPosition());
            // DebugHelper.Log(GetEntityCountInHashmap(quadrantMultiHashMap,
            //     GetPositionHashMapKey(UtilsClass.GetMouseWorldPosition())));

            quadrantMultiHashMap.Clear();
            if (_entityQuery.CalculateEntityCount() > quadrantMultiHashMap.Capacity)
            {
                quadrantMultiHashMap.Capacity = _entityQuery.CalculateEntityCount();
            }

            new SetQuadrantDataHashMapJob
            {
                QuadrantMultiHashMap = quadrantMultiHashMap.AsParallelWriter()
            }.Schedule(_entityQuery);
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
            return (int) (math.floor(position.x / QuadrantCellSize) +
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

            public void Execute(in Entity entity, in LocalTransform localTransform)
            {
                var hashMapKey = GetPositionHashMapKey(localTransform.Position);
                QuadrantMultiHashMap.Add(hashMapKey, new QuadrantData
                {
                    Entity = entity,
                    Position = localTransform.Position
                });
            }
        }
    }
}
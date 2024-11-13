using UnitBehaviours.Targeting;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.Pathing
{
    public partial struct TargetSelectorSystem : ISystem
    {
        private SystemHandle _quadrantSystemHandle;
        private EntityQuery _entityQuery;

        public void OnCreate(ref SystemState state)
        {
            _quadrantSystemHandle = state.World.GetExistingSystem(typeof(QuadrantSystem));
            _entityQuery = state.GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<TargetFollow>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var quadrantDataManager = SystemAPI.GetComponent<QuadrantDataManager>(_quadrantSystemHandle);
            var entityCount = _entityQuery.CalculateEntityCount();
            var entities = new NativeList<Entity>(entityCount, Allocator.TempJob);
            var positions = new NativeList<float3>(entityCount, Allocator.TempJob);
            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithEntityAccess()
                         .WithAll<TargetFollow>())
            {
                entities.Add(entity);
                positions.Add(localTransform.ValueRO.Position);
            }

            var job = new FindTargetJob
            {
                QuadrantMultiHashMap = quadrantDataManager.QuadrantMultiHashMap,
                Positions = positions.AsArray(),
                Entities = entities.AsArray(),
                TargetFollows = SystemAPI.GetComponentLookup<TargetFollow>()
            };
            var jobHandle = job.Schedule(entities.Length, 10);
            state.Dependency = jobHandle;

            entities.Dispose(state.Dependency);
            positions.Dispose(state.Dependency);
        }

        private struct FindTargetJob : IJobParallelFor
        {
            [ReadOnly] public NativeParallelMultiHashMap<int, QuadrantData> QuadrantMultiHashMap;
            [ReadOnly] public NativeArray<float3> Positions;
            [ReadOnly] public NativeArray<Entity> Entities;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<TargetFollow> TargetFollows;

            public void Execute(int index)
            {
                var position = Positions[index];
                var entity = Entities[index];
                var hashMapKey = QuadrantSystem.GetPositionHashMapKey(position);
                var closestTargetEntity = Entity.Null;
                var closestTargetDistance = float.MaxValue;

                // First check center
                FindTarget(hashMapKey, position, ref closestTargetEntity,
                    ref closestTargetDistance, entity);
                // Then search neighbours
                FindTarget(hashMapKey + 1, position, ref closestTargetEntity,
                    ref closestTargetDistance, entity);
                FindTarget(hashMapKey - 1, position, ref closestTargetEntity,
                    ref closestTargetDistance, entity);
                FindTarget(hashMapKey + QuadrantSystem.QuadrantYMultiplier, position, ref closestTargetEntity,
                    ref closestTargetDistance, entity);
                FindTarget(hashMapKey - QuadrantSystem.QuadrantYMultiplier, position, ref closestTargetEntity,
                    ref closestTargetDistance, entity);
                // Then search corners
                FindTarget(hashMapKey + 1 + QuadrantSystem.QuadrantYMultiplier, position, ref closestTargetEntity,
                    ref closestTargetDistance, entity);
                FindTarget(hashMapKey - 1 + QuadrantSystem.QuadrantYMultiplier, position, ref closestTargetEntity,
                    ref closestTargetDistance, entity);
                FindTarget(hashMapKey + 1 - QuadrantSystem.QuadrantYMultiplier, position, ref closestTargetEntity,
                    ref closestTargetDistance, entity);
                FindTarget(hashMapKey - 1 - QuadrantSystem.QuadrantYMultiplier, position, ref closestTargetEntity,
                    ref closestTargetDistance, entity);

                TargetFollows[entity] = new TargetFollow
                {
                    Target = closestTargetEntity,
                    CurrentDistanceToTarget = closestTargetDistance
                };
            }

            private void FindTarget(int hashMapKey, float3 position, ref Entity closestTargetEntity,
                ref float closestTargetDistance, Entity entity)
            {
                QuadrantData quadrantData;
                NativeParallelMultiHashMapIterator<int> nativeParallelMultiHashMapIterator;
                if (QuadrantMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData,
                        out nativeParallelMultiHashMapIterator))
                {
                    do
                    {
                        var distance = math.distance(position, quadrantData.Position);
                        if (distance < closestTargetDistance)
                        {
                            // Don't kill yourself, plz
                            if (entity != quadrantData.Entity)
                            {
                                closestTargetDistance = distance;
                                closestTargetEntity = quadrantData.Entity;
                            }
                        }
                    } while (QuadrantMultiHashMap.TryGetNextValue(out quadrantData,
                                 ref nativeParallelMultiHashMapIterator));
                }
            }
        }
    }
}
using UnitBehaviours.Targeting;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.Pathing
{
    public partial struct TargetSelectorSystem : ISystem
    {
        private SystemHandle _quadrantSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            _quadrantSystemHandle = state.World.GetExistingSystem(typeof(QuadrantSystem));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var quadrantDataManager = SystemAPI.GetComponent<QuadrantDataManager>(_quadrantSystemHandle);
            using var jobHandleList = new NativeList<JobHandle>(Allocator.Temp);
            foreach (var (_, _, entity) in SystemAPI
                         .Query<RefRO<TargetFollow>, RefRO<LocalTransform>>()
                         .WithAll<TargetSelector>().WithEntityAccess())
            {
                var job = new FindTargetJob
                {
                    QuadrantMultiHashMap = quadrantDataManager.QuadrantMultiHashMap,
                    LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                    TargetFollowLookup = SystemAPI.GetComponentLookup<TargetFollow>(),
                    Entity = entity
                };
                jobHandleList.Add(job.Schedule());
            }

            JobHandle.CompleteAll(jobHandleList.AsArray());
        }

        private struct FindTargetJob : IJob
        {
            [ReadOnly] public NativeParallelMultiHashMap<int, QuadrantData> QuadrantMultiHashMap;
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            public ComponentLookup<TargetFollow> TargetFollowLookup;
            public Entity Entity;

            public void Execute()
            {
                var position = LocalTransformLookup[Entity].Position;
                var hashMapKey = QuadrantSystem.GetPositionHashMapKey(position);
                var closestTargetEntity = Entity.Null;
                var closestTargetDistance = float.MaxValue;

                // First check center
                FindTarget(hashMapKey, position, ref closestTargetEntity,
                    ref closestTargetDistance);
                // Then search neighbours
                FindTarget(hashMapKey + 1, position, ref closestTargetEntity,
                    ref closestTargetDistance);
                FindTarget(hashMapKey - 1, position, ref closestTargetEntity,
                    ref closestTargetDistance);
                FindTarget(hashMapKey + QuadrantSystem.QuadrantYMultiplier, position, ref closestTargetEntity,
                    ref closestTargetDistance);
                FindTarget(hashMapKey - QuadrantSystem.QuadrantYMultiplier, position, ref closestTargetEntity,
                    ref closestTargetDistance);
                // Then search corners
                FindTarget(hashMapKey + 1 + QuadrantSystem.QuadrantYMultiplier, position, ref closestTargetEntity,
                    ref closestTargetDistance);
                FindTarget(hashMapKey - 1 + QuadrantSystem.QuadrantYMultiplier, position, ref closestTargetEntity,
                    ref closestTargetDistance);
                FindTarget(hashMapKey + 1 - QuadrantSystem.QuadrantYMultiplier, position, ref closestTargetEntity,
                    ref closestTargetDistance);
                FindTarget(hashMapKey - 1 - QuadrantSystem.QuadrantYMultiplier, position, ref closestTargetEntity,
                    ref closestTargetDistance);

                TargetFollowLookup[Entity] = new TargetFollow
                {
                    Target = closestTargetEntity,
                    CurrentDistanceToTarget = closestTargetDistance
                };
            }

            private void FindTarget(int hashMapKey, float3 position, ref Entity closestTargetEntity,
                ref float closestTargetDistance)
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
                            if (Entity != quadrantData.Entity)
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

        private bool TryGetClosestTarget(ref SystemState state, float3 position, out Entity target)
        {
            target = Entity.Null;
            var shortestTargetDistance = float.MaxValue;
            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Targetable>()
                         .WithEntityAccess())
            {
                var targetPosition = localTransform.ValueRO.Position;
                var distance = math.distance(position, targetPosition);
                if (distance < shortestTargetDistance)
                {
                    shortestTargetDistance = distance;
                    target = entity;
                }
            }

            return target != Entity.Null;
        }
    }
}
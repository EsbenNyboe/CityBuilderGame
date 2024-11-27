using UnitAgency;
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
    public struct TargetSelector : IComponentData
    {
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    [DisableAutoCreation]
    public partial struct TargetSelectorSystem : ISystem
    {
        private SystemHandle _quadrantSystemHandle;
        private EntityQuery _entityQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetSelector>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
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
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithEntityAccess()
                         .WithAll<TargetSelector>())
            {
                entities.Add(entity);
                positions.Add(localTransform.ValueRO.Position);
                // TODO: Add some safe-guard against add/remove-loop:
                ecb.RemoveComponent<TargetSelector>(entity);
                ecb.AddComponent<IsDeciding>(entity);
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

                // section not implemented..
                QuadrantSystem.TryFindClosestEntity(QuadrantMultiHashMap, hashMapKey, -1, position,
                    entity, out var closestTargetEntity, out var closestTargetDistance);

                TargetFollows[entity] = new TargetFollow
                {
                    Target = closestTargetEntity,
                    CurrentDistanceToTarget = closestTargetDistance
                };
            }
        }
    }
}
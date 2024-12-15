using UnitAgency;
using UnitState.Mood;
using Unity.Burst;
using Unity.Entities;

namespace UnitBehaviours.Idle
{
    public struct IsIdle : IComponentData
    {
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsIdleSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _query = state.GetEntityQuery(ComponentType.ReadOnly<IsIdle>(),
                ComponentType.ReadOnly<PathFollow>(),
                ComponentType.ReadWrite<MoodRestlessness>());
        }

        private const float MaxIdleTime = 1f;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            new IsIdleJob
            {
                EcbParallelWriter = ecb.AsParallelWriter()
            }.ScheduleParallel(_query);
        }

        [BurstCompile]
        private partial struct IsIdleJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter EcbParallelWriter;

            public void Execute(in Entity entity, in PathFollow pathFollow, ref MoodRestlessness moodRestlessness)
            {
                if (pathFollow.IsMoving())
                {
                    return;
                }

                if (moodRestlessness.Restlessness >= MaxIdleTime)
                {
                    moodRestlessness.Restlessness = 0;
                    EcbParallelWriter.RemoveComponent<IsIdle>(entity.Index, entity);
                    EcbParallelWriter.AddComponent(entity.Index, entity, new IsDeciding());
                }
            }
        }
    }
}
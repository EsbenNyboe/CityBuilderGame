using CustomTimeCore;
using Debugging;
using Grid;
using SystemGroups;
using UnitAgency.Data;
using UnitBehaviours.Pathing;
using UnitState.Mood;
using Unity.Burst;
using Unity.Entities;

namespace UnitBehaviours.Hunger
{
    [UpdateInGroup(typeof(UnitBehaviourGridWritingSystemGroup))]
    public partial struct IsEatingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<DebugToggleManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var hungerPerSecWhenEating = -0.2f * SystemAPI.Time.DeltaTime * timeScale;
            var gridManager = SystemAPI.GetSingleton<GridManager>();

            foreach (var (isEating,
                         pathFollow,
                         moodHunger,
                         entity) in SystemAPI
                         .Query<RefRO<IsEating>, RefRO<PathFollow>, RefRW<MoodHunger>>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    ecb.RemoveComponent<IsEating>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                if (moodHunger.ValueRO.Hunger > 0)
                {
                    moodHunger.ValueRW.Hunger -= hungerPerSecWhenEating;
                }
                else
                {
                    ecb.RemoveComponent<IsEating>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }

            SystemAPI.SetSingleton(gridManager);
        }
    }
}
using CustomTimeCore;
using UnitBehaviours.UnitConfigurators;
using UnitBehaviours.UnitManagers;
using UnitState.Mood;
using Unity.Entities;

namespace UnitBehaviours.Aging
{
    public partial struct AgeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<CustomTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var childHoodDuration = SystemAPI.GetSingleton<UnitBehaviourManager>().ChildHoodDuration;
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            foreach (var age in SystemAPI.Query<RefRW<Age>>())
            {
                age.ValueRW.AgeInSeconds += SystemAPI.Time.DeltaTime * timeScale;
            }

            foreach (var (age, moodLoneliness, moodSleepiness, entity) in SystemAPI.Query<RefRO<Age>, RefRO<MoodLoneliness>, RefRO<MoodSleepiness>>()
                         .WithAll<Baby>().WithEntityAccess())
            {
                // TODO: Extract threshold for sleepiness
                if (age.ValueRO.AgeInSeconds > childHoodDuration &&
                    moodLoneliness.ValueRO.Loneliness <= 0 &&
                    moodSleepiness.ValueRO.Sleepiness < 1)
                {
                    ecb.RemoveComponent<Baby>(entity);
                }
            }
        }
    }
}
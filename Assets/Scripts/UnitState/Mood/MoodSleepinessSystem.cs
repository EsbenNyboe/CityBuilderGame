using CustomTimeCore;
using SystemGroups;
using Unity.Burst;
using Unity.Entities;

namespace UnitState.Mood
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct MoodSleepinessSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            var sleepinessPerSecWhenIdle = 0.02f * SystemAPI.Time.DeltaTime * timeScale;

            foreach (var moodSleepiness in SystemAPI.Query<RefRW<MoodSleepiness>>())
            {
                moodSleepiness.ValueRW.Sleepiness += sleepinessPerSecWhenIdle;
            }
        }
    }
}
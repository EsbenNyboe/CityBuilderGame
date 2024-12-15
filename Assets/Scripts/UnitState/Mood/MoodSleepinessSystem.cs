using SystemGroups;
using Unity.Burst;
using Unity.Entities;

namespace UnitState.Mood
{
    public struct MoodSleepiness : IComponentData
    {
        public float Sleepiness;
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct MoodSleepinessSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var sleepinessPerSecWhenIdle = 0.02f * SystemAPI.Time.DeltaTime;

            foreach (var moodSleepiness in SystemAPI.Query<RefRW<MoodSleepiness>>())
            {
                moodSleepiness.ValueRW.Sleepiness += sleepinessPerSecWhenIdle;
            }
        }
    }
}
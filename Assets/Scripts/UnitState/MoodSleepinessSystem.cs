using Unity.Burst;
using Unity.Entities;
using ISystem = Unity.Entities.ISystem;
using SystemState = Unity.Entities.SystemState;

[UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
public partial struct MoodSleepinessSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var sleepinessPerSecWhenIdle = 0.02f * SystemAPI.Time.DeltaTime;

        foreach (var moodSleepiness in SystemAPI.Query<RefRW<MoodSleepiness>>().WithNone<IsSleeping>())
        {
            moodSleepiness.ValueRW.Sleepiness += sleepinessPerSecWhenIdle;
        }
    }
}
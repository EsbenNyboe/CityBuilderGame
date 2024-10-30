using UnitAgency;
using Unity.Burst;
using Unity.Entities;
using ISystem = Unity.Entities.ISystem;
using SystemState = Unity.Entities.SystemState;

public partial struct MoodSleepinessSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        var sleepinessPerSecWhenIdle = 0.02f * SystemAPI.Time.DeltaTime;
        var sleepinessPerSecWhenSleeping = -0.2f * SystemAPI.Time.DeltaTime;

        // TEMPORARY:
        foreach (var (moodSleepiness, entity) in SystemAPI.Query<RefRO<MoodSleepiness>>().WithEntityAccess().WithNone<IsSleeping>()
                     .WithNone<IsSeekingBed>())
        {
            if (moodSleepiness.ValueRO.Sleepiness > 0.1f)
            {
                // Feel a bit tired... maybe I can go to bed soon?
                ecb.AddComponent<IsDeciding>(entity);
            }
        }

        foreach (var (moodSleepiness, entity) in SystemAPI.Query<RefRW<MoodSleepiness>>().WithNone<IsSleeping>().WithEntityAccess())
        {
            moodSleepiness.ValueRW.Sleepiness += sleepinessPerSecWhenIdle;
            if (moodSleepiness.ValueRO.Sleepiness >= 1)
            {
                // Time to die!
                ecb.AddComponent<IsDeciding>(entity);
            }
        }

        ecb.Playback(state.EntityManager);
    }
}
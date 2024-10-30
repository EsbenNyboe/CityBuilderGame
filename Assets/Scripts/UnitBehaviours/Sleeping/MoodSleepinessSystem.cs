using UnitAgency;
using Unity.Burst;
using Unity.Entities;

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
                ecb.AddComponent<IsDecidingTag>(entity);
            }
        }

        foreach (var (moodSleepiness, entity) in SystemAPI.Query<RefRW<MoodSleepiness>>().WithNone<IsSleeping>().WithEntityAccess())
        {
            moodSleepiness.ValueRW.Sleepiness += sleepinessPerSecWhenIdle;
            if (moodSleepiness.ValueRO.Sleepiness >= 1)
            {
                // Time to die!
                ecb.AddComponent<IsDecidingTag>(entity);
            }
        }

        foreach (var (moodSleepiness, entity) in SystemAPI.Query<RefRW<MoodSleepiness>>().WithAll<IsSleeping>().WithEntityAccess())
        {
            moodSleepiness.ValueRW.Sleepiness += sleepinessPerSecWhenSleeping;
            if (moodSleepiness.ValueRO.Sleepiness <= 0)
            {
                // Time to get up!
                ecb.AddComponent<IsDecidingTag>(entity);
            }
        }

        ecb.Playback(state.EntityManager);
    }
}
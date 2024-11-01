using UnitAgency;
using Unity.Entities;

public struct IsIdle : IComponentData
{
}

[UpdateAfter(typeof(PathfindingSystem))]
public partial struct IsIdleSystem : ISystem
{
    private const float MaxIdleTime = 1f;

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

        foreach (var (moodRestlessness, entity) in SystemAPI.Query<RefRW<MoodRestlessness>>().WithEntityAccess().WithAll<IsIdle>())
        {
            if (moodRestlessness.ValueRO.TimeSpentDoingNothing >= MaxIdleTime)
            {
                moodRestlessness.ValueRW.TimeSpentDoingNothing = 0;
                ecb.RemoveComponent<IsIdle>(entity);
                ecb.AddComponent(entity, new IsDeciding());
            }
        }

        ecb.Playback(state.EntityManager);
    }
}
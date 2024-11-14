using UnitAgency;
using Unity.Entities;

public struct IsIdle : IComponentData
{
}


[UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
public partial struct IsIdleSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    private const float MaxIdleTime = 1f;

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (pathFollow, moodRestlessness, entity) in SystemAPI
                     .Query<RefRO<PathFollow>, RefRW<MoodRestlessness>>().WithEntityAccess()
                     .WithAll<IsIdle>())
        {
            if (pathFollow.ValueRO.IsMoving())
            {
                continue;
            }

            if (moodRestlessness.ValueRO.TimeSpentDoingNothing >= MaxIdleTime)
            {
                moodRestlessness.ValueRW.TimeSpentDoingNothing = 0;
                ecb.RemoveComponent<IsIdle>(entity);
                ecb.AddComponent(entity, new IsDeciding());
            }
        }
    }
}
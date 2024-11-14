using Unity.Entities;

[UpdateInGroup(typeof(LifetimeSystemGroup), OrderLast = true)]
public partial struct SpawnedUnitSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (_, entity) in SystemAPI.Query<RefRO<SpawnedUnit>>().WithEntityAccess())
        {
            ecb.RemoveComponent<SpawnedUnit>(entity);
        }
    }
}
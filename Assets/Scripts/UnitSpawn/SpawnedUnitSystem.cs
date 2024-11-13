using Unity.Entities;

[UpdateInGroup(typeof(LifetimeSystemGroup), OrderFirst = true)]
public partial struct SpawnedUnitSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

        foreach (var (_, entity) in SystemAPI.Query<RefRO<SpawnedUnit>>().WithEntityAccess())
        {
            ecb.RemoveComponent<SpawnedUnit>(entity);
        }

        ecb.Playback(state.EntityManager);
    }
}
using UnitAgency;
using Unity.Entities;

public struct IsIdle : IComponentData
{
}

public partial struct IsIdleSystem : ISystem
{
    private SystemHandle _tickManagerSystemHandle;

    public void OnCreate(ref SystemState state)
    {
        _tickManagerSystemHandle = state.EntityManager.World.GetExistingSystem(typeof(TickManagerSystem));
    }

    public void OnUpdate(ref SystemState state)
    {
        var isTickingThisFrame = SystemAPI.GetComponent<TickManager>(_tickManagerSystemHandle).IsTicking;
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        foreach (var (pathFollow, entity) in SystemAPI.Query<RefRO<PathFollow>>().WithEntityAccess().WithAll<IsIdle>())
        {
            if (pathFollow.ValueRO.IsMoving())
            {
                continue;
            }

            if (isTickingThisFrame)
            {
                // Debug.Log("START IDLE");
                ecb.RemoveComponent<IsIdle>(entity);
                ecb.AddComponent(entity, new IsDeciding());
            }
        }

        ecb.Playback(state.EntityManager);
    }
}
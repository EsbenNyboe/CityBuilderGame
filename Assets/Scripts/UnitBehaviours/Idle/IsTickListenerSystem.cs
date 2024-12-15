using UnitAgency;
using UnitBehaviours.Pathing;
using Unity.Burst;
using Unity.Entities;

namespace UnitBehaviours.Idle
{
    public struct IsTickListener : IComponentData
    {
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    [UpdateAfter(typeof(TickManagerSystem))]
    public partial struct IsTickListenerSystem : ISystem
    {
        private SystemHandle _tickManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _tickManagerSystemHandle = state.EntityManager.World.GetExistingSystem(typeof(TickManagerSystem));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var isTickingThisFrame = SystemAPI.GetComponent<TickManager>(_tickManagerSystemHandle).IsTicking;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (pathFollow, entity) in SystemAPI.Query<RefRO<PathFollow>>().WithEntityAccess()
                         .WithAll<IsTickListener>())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                if (isTickingThisFrame)
                {
                    ecb.RemoveComponent<IsTickListener>(entity);
                    ecb.AddComponent(entity, new IsDeciding());
                }
            }
        }
    }
}
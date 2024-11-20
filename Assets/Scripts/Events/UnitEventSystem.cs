using Unity.Entities;
using Unity.Mathematics;

namespace Events
{
    public struct DamageEvent : IComponentData
    {
        public float3 Position;
    }

    public struct DeathEvent : IComponentData
    {
        public float3 Position;
    }

    public partial class UnitEventSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);

            foreach (var (deathEvent, entity) in SystemAPI.Query<RefRO<DeathEvent>>()
                         .WithEntityAccess())
            {
                UnitEventManager.Instance.PlayDeathEffect(deathEvent.ValueRO.Position);
                SoundManager.Instance.PlayDeathSound(deathEvent.ValueRO.Position);
                ecb.DestroyEntity(entity);
            }

            foreach (var (damageEvent, entity) in SystemAPI.Query<RefRO<DamageEvent>>()
                         .WithEntityAccess())
            {
                UnitEventManager.Instance.PlayDamageEffect(damageEvent.ValueRO.Position);
                SoundManager.Instance.PlayDamageSound(damageEvent.ValueRO.Position);
                ecb.DestroyEntity(entity);
            }
        }
    }
}
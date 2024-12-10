using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Events
{
    public struct DamageEvent : IComponentData
    {
        public float3 Position;
        public UnitType TargetType;
    }

    public struct DeathEvent : IComponentData
    {
        public float3 Position;
        public UnitType TargetType;
    }

    public enum AttackType
    {
        Punch,
        Stab
    }

    public enum UnitType
    {
        Villager,
        Boar
    }

    public partial class UnitEventSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);

            const int maxDeathSoundsPerFrame = 100;
            var deathSounds = 0;
            foreach (var (deathEvent, entity) in SystemAPI.Query<RefRO<DeathEvent>>()
                         .WithEntityAccess())
            {
                ParticleEffectManager.Instance.PlayDeathEffect(deathEvent.ValueRO.Position);
                ecb.DestroyEntity(entity);

                // TODO: Make sure we're only playing sounds that are within audible range
                deathSounds++;
                if (deathSounds > maxDeathSoundsPerFrame)
                {
                    continue;
                }

                switch (deathEvent.ValueRO.TargetType)
                {
                    case UnitType.Villager:
                        SoundManager.Instance.PlayDeathSound(deathEvent.ValueRO.Position);
                        break;
                    case UnitType.Boar:
                        SoundManager.Instance.PlayBoarDeathSound(deathEvent.ValueRO.Position);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            foreach (var (damageEvent, entity) in SystemAPI.Query<RefRO<DamageEvent>>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                ParticleEffectManager.Instance.PlayDamageEffect(damageEvent.ValueRO.Position);
                SpriteEffectManager.Instance.PlayDamageEffect(damageEvent.ValueRO.Position);
                switch (damageEvent.ValueRO.TargetType)
                {
                    case UnitType.Villager:
                        SoundManager.Instance.PlayDamageSound(damageEvent.ValueRO.Position);
                        break;
                    case UnitType.Boar:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
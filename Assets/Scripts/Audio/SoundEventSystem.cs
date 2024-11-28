using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Audio
{
    public struct SoundEvent : IComponentData
    {
        public float3 Position;
        public SoundEventType Type;
    }

    public enum SoundEventType
    {
        SpearThrow,
        SpearHit,
        BoarCharge,
        BoarDeath
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct SoundEventSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            _query = state.GetEntityQuery(ComponentType.ReadOnly<SoundEvent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var soundEvent in SystemAPI.Query<RefRO<SoundEvent>>())
            {
                switch (soundEvent.ValueRO.Type)
                {
                    case SoundEventType.SpearThrow:
                        SoundManager.Instance.PlayAtPosition(SoundManager.Instance._spearThrowSound, soundEvent.ValueRO.Position);
                        break;
                    case SoundEventType.SpearHit:
                        SoundManager.Instance.PlayAtPosition(SoundManager.Instance._spearDamageSound, soundEvent.ValueRO.Position);
                        break;
                    case SoundEventType.BoarCharge:
                        SoundManager.Instance.PlayAtPosition(SoundManager.Instance._boarChargeSound, soundEvent.ValueRO.Position);
                        break;
                    case SoundEventType.BoarDeath:
                        SoundManager.Instance.PlayAtPosition(SoundManager.Instance._boarDeathSound, soundEvent.ValueRO.Position);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            state.EntityManager.DestroyEntity(_query);
        }
    }
}
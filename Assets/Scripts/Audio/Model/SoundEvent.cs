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
}
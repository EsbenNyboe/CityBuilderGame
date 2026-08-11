using Unity.Entities;
using Unity.Mathematics;

namespace Audio
{
    public struct SoundEvent : IComponentData
    {
        public float3 Position;
        public SoundEventType Type;

        public SoundEvent(float3 position, SoundEventType type)
        {
            Position = position;
            Type = type;
        }
    }

    public enum SoundEventType
    {
        SpearThrow,
        SpearHit,
        BoarCharge,
        BoarDeath,
        VillagerTalk,
        VillagerSleep,
        VillagerEat
    }
}
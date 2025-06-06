using Unity.Entities;
using Unity.Mathematics;

namespace UnitState.SocialState
{
    public struct SocialEvent : IComponentData
    {
        public Entity Perpetrator;
        public float3 Position;
        public float InfluenceAmount;
        public float InfluenceRadius;
    }

    /// <summary>
    ///     For this type of event there is a perpetrator, a victim (in the positive or negative sense :))
    ///     and some amount of observers (including perp and victim).
    ///     Based on how much a given observer (dis)liked the *victim*, their opinion of the *perpetrator*
    ///     will change. This means if you did something bad (e.g. murdered) to someone I like, I will
    ///     dislike you more, but if you did it to someone I hate, I will like you more.
    /// </summary>
    public struct SocialEventWithVictim : IComponentData
    {
        public Entity Perpetrator;
        public Entity Victim;
        public float3 Position;
        public float InfluenceAmount;
        public float InfluenceRadius;
    }
}
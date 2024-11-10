using UnitAgency;
using Unity.Entities;

namespace UnitBehaviours.Gossiping
{
    /// <summary>
    /// Indicates that we are standing around and waiting for someone to anyone to come and talk with us.
    /// </summary>
    public struct IsAvailableForTalking : IComponentData
    {
        public float PatienceSeconds;
    }

    public partial struct IsAvailableForTalkingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            using var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (isAvailableForTalking, entity) in SystemAPI.Query<RefRW<IsAvailableForTalking>>().WithEntityAccess())
            {
                isAvailableForTalking.ValueRW.PatienceSeconds -= SystemAPI.Time.DeltaTime;
                if (isAvailableForTalking.ValueRO.PatienceSeconds <= 0)
                {
                    ecb.RemoveComponent<IsAvailableForTalking>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}

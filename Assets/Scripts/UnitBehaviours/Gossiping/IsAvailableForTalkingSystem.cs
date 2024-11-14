using UnitAgency;
using Unity.Entities;

namespace UnitBehaviours.Gossiping
{
    /// <summary>
    ///     Indicates that we are standing around and waiting for someone to anyone to come and talk with us.
    /// </summary>
    public struct IsAvailableForTalking : IComponentData
    {
        public float PatienceSeconds;
    }

    public partial struct IsAvailableForTalkingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (isAvailableForTalking, entity) in SystemAPI.Query<RefRW<IsAvailableForTalking>>()
                         .WithEntityAccess())
            {
                isAvailableForTalking.ValueRW.PatienceSeconds -= SystemAPI.Time.DeltaTime;
                if (isAvailableForTalking.ValueRO.PatienceSeconds <= 0)
                {
                    ecb.RemoveComponent<IsAvailableForTalking>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }
        }
    }
}
using UnitAgency;
using UnitState;
using Unity.Entities;

namespace UnitBehaviours.Gossiping
{
    public struct IsTalking : IComponentData
    {
    }

    public partial struct IsTalkingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (loneliness, entity) in SystemAPI.Query<RefRO<MoodLoneliness>>().WithAll<IsTalking>()
                         .WithEntityAccess())
            {
                if (loneliness.ValueRO.Loneliness <= 0)
                {
                    ecb.RemoveComponent<IsTalking>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }
        }
    }
}
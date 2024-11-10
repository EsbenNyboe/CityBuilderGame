using UnitAgency;
using Unity.Entities;

namespace UnitBehaviours.Gossiping
{
    public struct IsTalking : IComponentData { }

    public partial struct IsTalkingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            using var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (loneliness, entity) in SystemAPI.Query<RefRO<MoodLoneliness>>().WithAll<IsTalking>().WithEntityAccess())
            {
                if (loneliness.ValueRO.Value <= 0)
                {
                    ecb.RemoveComponent<IsTalking>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }
            
            ecb.Playback(state.EntityManager);
        }
    }
}

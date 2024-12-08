using Unity.Burst;
using Unity.Entities;

namespace UnitBehaviours.Talking
{
    public struct ConversationEvent : IComponentData
    {
        public Entity Initiator;
        public Entity Target;
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup), OrderFirst = true)]
    public partial struct ConversationEventSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _query = state.GetEntityQuery(ComponentType.ReadOnly<ConversationEvent>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            ecb.DestroyEntity(_query, EntityQueryCaptureMode.AtPlayback);
        }
    }
}
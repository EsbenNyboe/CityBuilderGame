using Unity.Burst;
using Unity.Entities;

namespace UnitAgency
{
    internal partial struct UnitAgencySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Following the example at: https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-entity-command-buffer-automatic-playback.html
            var ecbSystemSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var commands = ecbSystemSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<IsDecidingTag>>().WithEntityAccess())
            {
                commands.RemoveComponent<IsDecidingTag>(entity);
                DecideNextBehaviour(entity, commands);
            }
        }

        private void DecideNextBehaviour(Entity entity, EntityCommandBuffer commands)
        {
            // If has log in hands: add "IsReturning" behaviour...
            // If is sleepy: add "IsSeekingBed" behaviour...
            // etc...
        }
    }
}

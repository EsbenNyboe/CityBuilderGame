using Unity.Entities;
using Unity.Mathematics;

namespace Rendering
{
    public struct DeathAnimationEvent : IComponentData
    {
        public float3 Position;
    }

    public partial class DeathAnimationEventSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);

            foreach (var (deathAnimationEvent, entity) in SystemAPI.Query<RefRO<DeathAnimationEvent>>()
                         .WithEntityAccess())
            {
                DeathAnimationManager.Instance.PlayDeathAnimation(deathAnimationEvent.ValueRO.Position);
                SoundManager.Instance.PlayDieSound(deathAnimationEvent.ValueRO.Position);
                ecb.DestroyEntity(entity);
            }
        }
    }
}
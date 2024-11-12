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
            var ecb = new EntityCommandBuffer(WorldUpdateAllocator);

            foreach (var (deathAnimationEvent, entity) in SystemAPI.Query<RefRO<DeathAnimationEvent>>()
                         .WithEntityAccess())
            {
                DeathAnimationManager.Instance.PlayDeathAnimation(deathAnimationEvent.ValueRO.Position);
                SoundManager.Instance.PlayDieSound(deathAnimationEvent.ValueRO.Position);
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);
        }
    }
}
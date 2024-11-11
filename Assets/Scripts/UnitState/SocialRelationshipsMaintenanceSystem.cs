using Unity.Entities;
using Unity.Transforms;

namespace UnitState
{
    [UpdateInGroup(typeof(SpawningSystemGroup), OrderFirst = true)]
    public partial struct SocialRelationshipsMaintenanceSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            RemoveDeletedUnitsFromExistingRelationships(ref state);
            CleanupDeletedUnits(ref state);

            RuminateOnAnimosity(ref state);
        }

        private void RuminateOnAnimosity(ref SystemState state)
        {
            foreach (var socialRelationships in SystemAPI.Query<RefRW<SocialRelationships>>())
            {
                socialRelationships.ValueRW.TimeSinceAnimosityStarted = socialRelationships.ValueRO.HasAnimosity
                    ? socialRelationships.ValueRO.TimeSinceAnimosityStarted + SystemAPI.Time.DeltaTime
                    : 0;
            }
        }

        private void RemoveDeletedUnitsFromExistingRelationships(ref SystemState state)
        {
            // UPDATE EXISTING RELATIONSHIPS
            using var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var socialRelationships in SystemAPI.Query<RefRW<SocialRelationships>>().WithAll<LocalTransform>())
            {
                foreach (var (_, destroyedEntity) in SystemAPI.Query<RefRO<SocialRelationships>>()
                             .WithNone<LocalTransform>().WithEntityAccess())
                {
                    socialRelationships.ValueRW.Relationships.Remove(destroyedEntity);
                }
            }

            ecb.Playback(state.EntityManager);
        }

        private void CleanupDeletedUnits(ref SystemState state)
        {
            // CLEANUP SOCIAL RELATIONSHIPS
            using var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (socialRelationships, entity) in SystemAPI.Query<RefRW<SocialRelationships>>()
                         .WithNone<LocalTransform>().WithEntityAccess())
            {
                socialRelationships.ValueRW.Relationships.Dispose();
                ecb.RemoveComponent<SocialRelationships>(entity);
                ecb.RemoveComponent<Child>(entity);
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
using UnitAgency;
using UnitState;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace UnitBehaviours.Talking
{
    /// <summary>
    ///     Indicates that we are pathfinding to someone else, usually if we wanted to be social, but there was no one close by
    /// </summary>
    public struct IsSeekingTalkingPartner : IComponentData
    {
        public bool HasStartedMoving;
    }

    public partial struct IsSeekingTalkingPartnerSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = GetEntityCommandBuffer(ref state);
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);

            foreach (var (localTransform, relationships, pathFollow, seekingTalkingPartner, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRW<SocialRelationships>, RefRO<PathFollow>,
                             RefRW<IsSeekingTalkingPartner>>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                if (seekingTalkingPartner.ValueRW.HasStartedMoving)
                {
                    // I reached my target after having started moving earlier. This means I am where the people are :D 
                    ecb.RemoveComponent<IsSeekingTalkingPartner>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                // I havent started moving yet...
                // Find a random person to walk to (this will be biased towards populated areas)
                var index = gridManager.Random.NextInt(0, relationships.ValueRO.Relationships.Count);
                var otherUnit = relationships.ValueRW.Relationships.GetKeyArray(Allocator.Temp)[index];

                var targetWorldPosition = SystemAPI.GetComponent<LocalTransform>(otherUnit).Position;
                var startWorldPosition = localTransform.ValueRO.Position;

                var startPosition = GridHelpers.GetXY(startWorldPosition);
                var targetPosition = GridHelpers.GetXY(targetWorldPosition);

                PathHelpers.TrySetPath(ecb, entity, startPosition, targetPosition);
                seekingTalkingPartner.ValueRW.HasStartedMoving = true;
            }
        }

        [BurstCompile]
        private EntityCommandBuffer GetEntityCommandBuffer(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            return ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        }
    }
}
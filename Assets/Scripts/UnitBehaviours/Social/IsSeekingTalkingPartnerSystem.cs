using UnitAgency;
using UnitBehaviours.Targeting;
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
            state.RequireForUpdate<QuadrantDataManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var quadrantDataManager = SystemAPI.GetSingleton<QuadrantDataManager>();

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
                // Find a random nearby person to walk to.
                var position = localTransform.ValueRO.Position;
                var cell = GridHelpers.GetXY(position);

                var hashMapKey = QuadrantSystem.GetPositionHashMapKey(position);

                // TODO: Check if seeker entity and target entity are in the same Section
                var section = gridManager.GetSection(cell);
                if (!QuadrantSystem.TryFindClosestEntity(quadrantDataManager.QuadrantMultiHashMap, hashMapKey,
                        section, position, entity, out var otherUnit, out _))
                {
                    // No people nearby. I'll find a random person to walk to.
                    var index = gridManager.Random.NextInt(0, relationships.ValueRO.Relationships.Count);
                    otherUnit = relationships.ValueRW.Relationships.GetKeyArray(Allocator.Temp)[index];
                }

                var targetPosition = SystemAPI.GetComponent<LocalTransform>(otherUnit).Position;


                var targetCell = GridHelpers.GetXY(targetPosition);

                PathHelpers.TrySetPath(ecb, entity, cell, targetCell);
                seekingTalkingPartner.ValueRW.HasStartedMoving = true;
            }
        }
    }
}
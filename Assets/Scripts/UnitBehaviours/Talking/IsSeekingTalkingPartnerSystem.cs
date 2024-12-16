using Grid;
using UnitAgency.Data;
using UnitBehaviours.Pathing;
using UnitBehaviours.Targeting;
using UnitState.SocialState;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

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
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<QuadrantDataManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var quadrantDataManager = SystemAPI.GetSingleton<QuadrantDataManager>();

            foreach (var (localTransform, socialRelationships, pathFollow, seekingTalkingPartner, entity) in SystemAPI
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
                // Find a nearby friend to walk to.
                var position = localTransform.ValueRO.Position;
                var cell = GridHelpers.GetXY(position);

                var quadrantsToSearch = 9;
                if (!QuadrantSystem.TryFindClosestFriend(socialRelationships.ValueRO,
                        quadrantDataManager.VillagerQuadrantMap, gridManager,
                        quadrantsToSearch, position, entity,
                        out var otherUnit, out _) &&
                    !QuadrantSystem.TryFindClosestEntity(quadrantDataManager.VillagerQuadrantMap, gridManager,
                        quadrantsToSearch, position, entity,
                        out otherUnit, out _))
                {
                    // No units nearby. I'll find a random person to walk to.
                    var relationships = socialRelationships.ValueRO.Relationships;
                    var index = gridManager.Random.NextInt(0, relationships.Count());
                    otherUnit = socialRelationships.ValueRW.Relationships.GetKeyArray(Allocator.Temp)[index];
                }

                var targetPosition = SystemAPI.GetComponent<LocalTransform>(otherUnit).Position;
                var targetCell = GridHelpers.GetXY(targetPosition);

                if (!gridManager.IsMatchingSection(cell, targetCell))
                {
                    // I have no one to talk to!
                    ecb.RemoveComponent<IsSeekingTalkingPartner>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                if (!gridManager.TryGetVacantHorizontalNeighbour(targetCell, out var pathTargetCell))
                {
                    if (!gridManager.TryGetClosestVacantCell(cell, targetCell, out pathTargetCell))
                    {
                        if (!gridManager.TryGetClosestWalkableCell(targetCell, out pathTargetCell, false, false))
                        {
                            Debug.LogError("How hard is it to find a talking partner?!");
                        }
                    }
                }

                PathHelpers.TrySetPath(ecb, gridManager, entity, cell, pathTargetCell);
                seekingTalkingPartner.ValueRW.HasStartedMoving = true;
            }
        }
    }
}
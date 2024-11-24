using UnitBehaviours;
using UnitBehaviours.AutonomousHarvesting;
using UnitBehaviours.Pathing;
using UnitBehaviours.Sleeping;
using UnitBehaviours.Talking;
using UnitBehaviours.Targeting;
using UnitState;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitAgency
{
    /// <summary>
    ///     Tag component to signify the entity is ready to decide its next behaviour.
    ///     It will be picked up by the <see cref="IsDecidingSystem" /> and
    ///     removed as a new behaviour is selected.
    /// </summary>
    public struct IsDeciding : IComponentData
    {
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup), OrderLast = true)]
    internal partial struct IsDecidingSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;
        private EntityQuery _query;
        private AttackAnimationManager _attackAnimationManager;
        private ComponentLookup<IsTalking> _isTalkingLookup;
        private ComponentLookup<IsTalkative> _isTalkativeLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<QuadrantDataManager>();
            state.RequireForUpdate<SocialDynamicsManager>();
            state.RequireForUpdate<AttackAnimationManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
            _query = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<IsDeciding>());
            _isTalkativeLookup = SystemAPI.GetComponentLookup<IsTalkative>();
            _isTalkingLookup = SystemAPI.GetComponentLookup<IsTalking>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _isTalkingLookup.Update(ref state);
            _isTalkativeLookup.Update(ref state);
            _attackAnimationManager = SystemAPI.GetSingleton<AttackAnimationManager>();
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var socialDynamicsManager = SystemAPI.GetSingleton<SocialDynamicsManager>();
            var quadrantDataManager = SystemAPI.GetSingleton<QuadrantDataManager>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, pathFollow,
                         inventory,
                         socialRelationships,
                         moodSleepiness,
                         moodLoneliness,
                         moodInitiative,
                         entity)
                     in SystemAPI
                         .Query<RefRO<IsDeciding>, RefRO<PathFollow>, RefRO<Inventory>, RefRO<SocialRelationships>,
                             RefRO<MoodSleepiness>,
                             RefRO<MoodLoneliness>,
                             RefRW<MoodInitiative>>()
                         .WithEntityAccess().WithNone<Pathfinding>().WithNone<AttackAnimation>())
            {
                ecb.RemoveComponent<IsDeciding>(entity);
                DecideNextBehaviour(ref state, gridManager, socialDynamicsManager, quadrantDataManager, ecb, pathFollow,
                    inventory,
                    socialRelationships,
                    moodSleepiness, moodLoneliness, moodInitiative, entity);
            }
        }

        private void DecideNextBehaviour(ref SystemState state,
            GridManager gridManager,
            SocialDynamicsManager socialDynamicsManager,
            QuadrantDataManager quadrantDataManager,
            EntityCommandBuffer ecb,
            RefRO<PathFollow> pathFollow,
            RefRO<Inventory> inventory,
            RefRO<SocialRelationships> socialRelationships,
            RefRO<MoodSleepiness> moodSleepiness,
            RefRO<MoodLoneliness> moodLoneliness,
            RefRW<MoodInitiative> moodInitiative,
            Entity entity
        )
        {
            var unitPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;
            var cell = GridHelpers.GetXY(unitPosition);
            var section = gridManager.GetSection(cell);

            var isSleepy = moodSleepiness.ValueRO.Sleepiness > 0.2f;
            var isMoving = pathFollow.ValueRO.IsMoving();
            // TODO: How do we find who's the annoying dude? 
            var isLonely = moodLoneliness.ValueRO.Loneliness > 1f;
            const float friendFactor = 1f;

            if (HasLogOfWood(inventory.ValueRO))
            {
                ecb.AddComponent(entity, new IsSeekingDropPoint());
            }
            else if (isMoving)
            {
                ecb.AddComponent(entity, new IsIdle());
            }
            else if (IsAnnoyedAtSomeone(
                         ref state,
                         entity,
                         socialRelationships.ValueRO.Relationships,
                         socialDynamicsManager.ThresholdForBecomingAnnoying,
                         unitPosition,
                         quadrantDataManager,
                         out var annoyingDude))
            {
                var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
                var annoyingDudePosition = transformLookup[annoyingDude].Position;
                var distanceToTarget = math.distance(unitPosition, annoyingDudePosition);
                if (distanceToTarget <= IsAttemptingMurderSystem.AttackRange)
                {
                    ecb.AddComponent(entity, new IsMurdering
                    {
                        Target = annoyingDude
                    });
                    ecb.AddComponent(entity, new AttackAnimation(new int2(-1), -1));
                }
                else
                {
                    ecb.AddComponent(entity, new IsAttemptingMurder());
                    ecb.SetComponent(entity, new TargetFollow
                    {
                        Target = annoyingDude,
                        CurrentDistanceToTarget = distanceToTarget,
                        DesiredRange = IsAttemptingMurderSystem.AttackRange
                    });
                }
            }
            else if (isSleepy)
            {
                if (gridManager.IsBedAvailableToUnit(unitPosition, entity))
                {
                    ecb.AddComponent(entity, new IsSleeping());
                }
                else
                {
                    ecb.AddComponent(entity, new IsSeekingBed());
                }
            }
            else if (IsAdjacentToTree(ref state, gridManager, cell, out var tree))
            {
                ecb.AddComponent(entity, new IsHarvesting());
                ecb.AddComponent(entity, new AttackAnimation(tree));
            }
            else if (isLonely)
            {
                if ((TalkingHelpers.TryGetNeighbourWithComponent(gridManager, cell, _isTalkativeLookup,
                         out var neighbour) ||
                     TalkingHelpers.TryGetNeighbourWithComponent(gridManager, cell, _isTalkingLookup, out neighbour)) &&
                    gridManager.TryGetOccupant(neighbour, out var neighbourEntity) &&
                    (socialRelationships.ValueRO.Relationships[neighbourEntity] > friendFactor ||
                     !QuadrantSystem.TryFindClosestFriend(socialRelationships.ValueRO,
                         quadrantDataManager.QuadrantMultiHashMap, QuadrantSystem.GetPositionHashMapKey(unitPosition),
                         section, unitPosition, entity, out _, out _)))
                {
                    ecb.AddComponent(entity, new IsTalking());
                    ecb.SetComponentEnabled<IsTalking>(entity, false);

                    ecb.AddComponent(ecb.CreateEntity(), new ConversationEvent
                    {
                        Initiator = entity,
                        Target = neighbourEntity
                    });
                }
                else if (moodInitiative.ValueRO.HasInitiative())
                {
                    moodInitiative.ValueRW.UseInitiative();
                    ecb.AddComponent(entity, new IsSeekingTalkingPartner());
                }
                else
                {
                    ecb.AddComponent(entity, new IsTalkative
                    {
                        Patience = 1
                    });
                }
            }
            else
            {
                ecb.AddComponent(entity, new IsSeekingTree());
            }
        }

        private bool IsAnnoyedAtSomeone(
            ref SystemState state,
            Entity self,
            NativeHashMap<Entity, float> relationships,
            float thresholdForBecomingAnnoying,
            float3 position,
            QuadrantDataManager quadrantDataManager,
            out Entity annoyingDude
        )
        {
            var quadrant = QuadrantSystem.GetPositionHashMapKey(position);
            if (!quadrantDataManager.QuadrantMultiHashMap.TryGetFirstValue(quadrant, out var data, out var iterator))
            {
                annoyingDude = Entity.Null;
                return false;
            }

            // We check some fixed amount of people in our quadrant as a semi-random way to get some sample
            var peopleLeftToCheck = 5;
            do
            {
                // If self, skip without 
                if (data.Entity == self)
                {
                    continue;
                }

                // Are we mad with this person
                // TODO: Don't consider folks not in our section on the grid
                if (relationships[data.Entity] < thresholdForBecomingAnnoying)
                {
                    annoyingDude = data.Entity;
                    return true;
                }

                peopleLeftToCheck--;
            } while (peopleLeftToCheck > 0 &&
                     quadrantDataManager.QuadrantMultiHashMap.TryGetNextValue(out data, ref iterator));

            annoyingDude = Entity.Null;
            return false;
        }

        private bool HasLogOfWood(Inventory inventory)
        {
            return inventory.CurrentItem == InventoryItem.LogOfWood;
        }

        private bool IsAdjacentToTree(ref SystemState state, GridManager gridManager, int2 cell, out int2 tree)
        {
            var foundTree = gridManager.TryGetNeighbouringTreeCell(cell, out tree);
            return foundTree;
        }
    }
}
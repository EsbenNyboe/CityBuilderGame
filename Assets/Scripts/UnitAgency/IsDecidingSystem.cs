using UnitBehaviours;
using UnitBehaviours.AutonomousHarvesting;
using UnitBehaviours.Pathing;
using UnitBehaviours.Sleeping;
using UnitBehaviours.Talking;
using UnitBehaviours.Targeting;
using UnitSpawn;
using UnitState;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
        private EntityQuery _query;
        private EntityQuery _boarQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<QuadrantDataManager>();
            state.RequireForUpdate<SocialDynamicsManager>();
            state.RequireForUpdate<AttackAnimationManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _boarQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Boar>(), ComponentType.ReadOnly<LocalTransform>());
            _query = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Villager>(),
                ComponentType.ReadOnly<IsDeciding>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<PathFollow>(),
                ComponentType.ReadOnly<Inventory>(),
                ComponentType.ReadOnly<MoodSleepiness>(),
                ComponentType.ReadOnly<MoodLoneliness>(),
                ComponentType.ReadWrite<MoodInitiative>(),
                ComponentType.ReadWrite<RandomContainer>(),
                ComponentType.Exclude<Pathfinding>(),
                ComponentType.Exclude<AttackAnimation>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var socialDynamicsManager = SystemAPI.GetSingleton<SocialDynamicsManager>();
            var quadrantDataManager = SystemAPI.GetSingleton<QuadrantDataManager>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var boarEntities = _boarQuery.ToEntityArray(Allocator.TempJob);
            var boarTransforms = _boarQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            var decideNextBehaviourJob = new DecideNextBehaviourJob
            {
                EcbParallelWriter = ecb.AsParallelWriter(),
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                GridManager = gridManager,
                SocialDynamicsManager = socialDynamicsManager,
                QuadrantDataManager = quadrantDataManager,
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                IsTalkativeLookup = SystemAPI.GetComponentLookup<IsTalkative>(),
                IsTalkingLookup = SystemAPI.GetComponentLookup<IsTalking>(),
                SocialRelationshipsLookup = SystemAPI.GetComponentLookup<SocialRelationships>(),
                BoarEntities = boarEntities,
                BoarTransforms = boarTransforms
            };
            decideNextBehaviourJob.Run(_query);

            boarEntities.Dispose();
            boarTransforms.Dispose();
        }

        [BurstCompile]
        private partial struct DecideNextBehaviourJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter EcbParallelWriter;
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public GridManager GridManager;
            [ReadOnly] public SocialDynamicsManager SocialDynamicsManager;
            [ReadOnly] public QuadrantDataManager QuadrantDataManager;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<IsTalkative> IsTalkativeLookup;
            [ReadOnly] public ComponentLookup<IsTalking> IsTalkingLookup;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<SocialRelationships> SocialRelationshipsLookup;

            [ReadOnly] public NativeArray<Entity> BoarEntities;
            [ReadOnly] public NativeArray<LocalTransform> BoarTransforms;

            public void Execute([EntityIndexInQuery] int i,
                in Entity entity,
                in LocalTransform localTransform,
                in PathFollow pathFollow,
                in Inventory inventory,
                in MoodSleepiness moodSleepiness,
                in MoodLoneliness moodLoneliness,
                ref MoodInitiative moodInitiative,
                ref RandomContainer randomContainer)
            {
                EcbParallelWriter.RemoveComponent<IsDeciding>(i, entity);

                var unitPosition = localTransform.Position;
                var cell = GridHelpers.GetXY(unitPosition);
                var section = GridManager.GetSection(cell);

                var isSleepy = moodSleepiness.Sleepiness > 0.2f;
                var isMoving = pathFollow.IsMoving();
                var isLonely = moodLoneliness.Loneliness > 1f;
                var hasInitiative = moodInitiative.HasInitiative();
                const float friendFactor = 1f;

                var socialRelationships = SocialRelationshipsLookup[entity];

                if (hasInitiative && BoarIsNearby(unitPosition, BoarTransforms, BoarEntities, out var nearbyBoar))
                {
                    var randomDelay = randomContainer.Random.NextFloat(0, 1);
                    moodInitiative.UseInitiative();
                    EcbParallelWriter.AddComponent<IsHoldingSpear>(i, entity);
                    EcbParallelWriter.AddComponent(i, entity, new IsThrowingSpear
                    {
                        Target = nearbyBoar
                    });
                    EcbParallelWriter.SetComponent(i, entity, new ActionGate
                    {
                        MinTimeOfAction = ElapsedTime + randomDelay
                    });
                }
                else if (HasLogOfWood(inventory))
                {
                    EcbParallelWriter.AddComponent(i, entity, new IsSeekingDropPoint());
                }
                else if (isMoving)
                {
                    EcbParallelWriter.AddComponent(i, entity, new IsIdle());
                }
                else if (IsAnnoyedAtSomeone(
                             entity,
                             socialRelationships.Relationships,
                             SocialDynamicsManager.ThresholdForBecomingAnnoying,
                             unitPosition,
                             QuadrantDataManager,
                             out var annoyingDude))
                {
                    var annoyingDudePosition = LocalTransformLookup[annoyingDude].Position;
                    var distanceToTarget = math.distance(unitPosition, annoyingDudePosition);
                    if (distanceToTarget <= IsAttemptingMurderSystem.AttackRange)
                    {
                        EcbParallelWriter.AddComponent(i, entity, new IsMurdering
                        {
                            Target = annoyingDude
                        });
                        EcbParallelWriter.AddComponent(i, entity, new AttackAnimation(new int2(-1), -1));
                    }
                    else
                    {
                        EcbParallelWriter.AddComponent(i, entity, new IsAttemptingMurder());
                        EcbParallelWriter.SetComponent(i, entity, new TargetFollow
                        {
                            Target = annoyingDude,
                            CurrentDistanceToTarget = distanceToTarget,
                            DesiredRange = IsAttemptingMurderSystem.AttackRange
                        });
                    }
                }
                else if (isSleepy && GridManager.IsBedAvailableToUnit(unitPosition, entity))
                {
                    EcbParallelWriter.AddComponent(i, entity, new IsSleeping());
                }
                else
                {
                    if (isSleepy && hasInitiative)
                    {
                        EcbParallelWriter.AddComponent(i, entity, new IsSeekingBed());
                    }
                    else if (IsAdjacentToTree(GridManager, cell, out var tree))
                    {
                        EcbParallelWriter.AddComponent(i, entity, new IsHarvesting());
                        EcbParallelWriter.AddComponent(i, entity, new AttackAnimation(tree));
                    }
                    else if (isLonely)
                    {
                        if ((TalkingHelpers.TryGetNeighbourWithComponent(GridManager, cell, IsTalkativeLookup,
                                 out var neighbour) ||
                             TalkingHelpers.TryGetNeighbourWithComponent(GridManager, cell, IsTalkingLookup, out neighbour)) &&
                            GridManager.TryGetOccupant(neighbour, out var neighbourEntity) &&
                            (socialRelationships.Relationships[neighbourEntity] > friendFactor ||
                             !QuadrantSystem.TryFindClosestFriend(socialRelationships,
                                 QuadrantDataManager.QuadrantMultiHashMap, QuadrantSystem.GetPositionHashMapKey(unitPosition),
                                 section, unitPosition, entity, out _, out _)))
                        {
                            EcbParallelWriter.AddComponent(i, entity, new IsTalking());
                            EcbParallelWriter.SetComponentEnabled<IsTalking>(i, entity, false);

                            EcbParallelWriter.AddComponent(i, EcbParallelWriter.CreateEntity(i), new ConversationEvent
                            {
                                Initiator = entity,
                                Target = neighbourEntity
                            });
                        }
                        else if (hasInitiative)
                        {
                            moodInitiative.UseInitiative();
                            EcbParallelWriter.AddComponent(i, entity, new IsSeekingTalkingPartner());
                        }
                        else
                        {
                            EcbParallelWriter.AddComponent(i, entity, new IsTalkative
                            {
                                Patience = 1
                            });
                        }
                    }
                    else if (hasInitiative)
                    {
                        moodInitiative.UseInitiative();
                        EcbParallelWriter.AddComponent(i, entity, new IsSeekingTree());
                    }
                    else
                    {
                        EcbParallelWriter.AddComponent<IsIdle>(i, entity);
                    }
                }
            }
        }

        private static bool BoarIsNearby(float3 unitPosition, NativeArray<LocalTransform> boarTransforms, NativeArray<Entity> boarEntities,
            out Entity nearbyBoar)
        {
            var threshold = 20f;
            for (var index = 0; index < boarTransforms.Length; index++)
            {
                var boarPosition = boarTransforms[index].Position;
                var distance = math.distance(unitPosition, boarPosition);
                if (distance < threshold)
                {
                    nearbyBoar = boarEntities[index];
                    return true;
                }
            }

            nearbyBoar = Entity.Null;
            return false;
        }

        private static bool IsAnnoyedAtSomeone(
            Entity self,
            NativeParallelHashMap<Entity, float> relationships,
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

        private static bool HasLogOfWood(Inventory inventory)
        {
            return inventory.CurrentItem == InventoryItem.LogOfWood;
        }

        private static bool IsAdjacentToTree(GridManager gridManager, int2 cell, out int2 tree)
        {
            var foundTree = gridManager.TryGetNeighbouringTreeCell(cell, out tree);
            return foundTree;
        }
    }
}
using CustomTimeCore;
using Grid;
using Inventory;
using SpriteTransformNS;
using SystemGroups;
using UnitAgency.Data;
using UnitBehaviours.ActionGateNS;
using UnitBehaviours.AutonomousHarvesting;
using UnitBehaviours.Hunger;
using UnitBehaviours.Idle;
using UnitBehaviours.Pathing;
using UnitBehaviours.Sleeping;
using UnitBehaviours.Talking;
using UnitBehaviours.Targeting;
using UnitBehaviours.Targeting.Core;
using UnitBehaviours.UnitConfigurators;
using UnitBehaviours.UnitManagers;
using UnitState.Mood;
using UnitState.SocialState;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitAgency.Logic
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup), OrderLast = true)]
    internal partial struct IsDecidingSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<QuadrantDataManager>();
            state.RequireForUpdate<SocialDynamicsManager>();
            state.RequireForUpdate<AttackAnimationManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _query = state.GetEntityQuery(ComponentType.ReadOnly<Villager>(),
                ComponentType.ReadOnly<IsDeciding>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<PathFollow>(),
                ComponentType.ReadWrite<InventoryState>(),
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
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var socialDynamicsManager = SystemAPI.GetSingleton<SocialDynamicsManager>();
            var quadrantDataManager = SystemAPI.GetSingleton<QuadrantDataManager>();
            var unitBehaviourManager = SystemAPI.GetSingleton<UnitBehaviourManager>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var decideNextBehaviourJob = new DecideNextBehaviourJob
            {
                EcbParallelWriter = ecb.AsParallelWriter(),
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                TimeScale = timeScale,
                GridManager = gridManager,
                SocialDynamicsManager = socialDynamicsManager,
                QuadrantDataManager = quadrantDataManager,
                UnitBehaviourManager = unitBehaviourManager,
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                IsTalkativeLookup = SystemAPI.GetComponentLookup<IsTalkative>(),
                IsTalkingLookup = SystemAPI.GetComponentLookup<IsTalking>(),
                SocialRelationshipsLookup = SystemAPI.GetComponentLookup<SocialRelationships>(),
                BabiesLookup = SystemAPI.GetComponentLookup<Baby>(),
                MoodHungerLookup = SystemAPI.GetComponentLookup<MoodHunger>()
            };
            decideNextBehaviourJob.ScheduleParallel(_query, state.Dependency).Complete();
        }

        [BurstCompile]
        private partial struct DecideNextBehaviourJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter EcbParallelWriter;
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public float TimeScale;
            [ReadOnly] public GridManager GridManager;
            [ReadOnly] public SocialDynamicsManager SocialDynamicsManager;
            [ReadOnly] public QuadrantDataManager QuadrantDataManager;
            [ReadOnly] public UnitBehaviourManager UnitBehaviourManager;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<IsTalkative> IsTalkativeLookup;
            [ReadOnly] public ComponentLookup<IsTalking> IsTalkingLookup;
            [ReadOnly] public ComponentLookup<Baby> BabiesLookup;
            [ReadOnly] public ComponentLookup<MoodHunger> MoodHungerLookup;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<SocialRelationships> SocialRelationshipsLookup;

            public void Execute([EntityIndexInQuery] int i,
                in Entity entity,
                in LocalTransform localTransform,
                in PathFollow pathFollow,
                ref InventoryState inventory,
                in MoodSleepiness moodSleepiness,
                in MoodLoneliness moodLoneliness,
                ref MoodInitiative moodInitiative,
                ref RandomContainer randomContainer)
            {
                EcbParallelWriter.RemoveComponent<IsDeciding>(i, entity);

                var position = localTransform.Position;
                var cell = GridHelpers.GetXY(position);
                var section = GridManager.GetSection(cell);

                // TODO: Convert these into "rings of quadrants to search" instead of "quadrants to search"
                var friendQuadrantsToSearch = 25;
                var boarQuadrantsToSearch = 9;
                var itemQuadrantsToSearch = UnitBehaviourManager.QuadrantSearchRange;


                var isBaby = BabiesLookup.HasComponent(entity);
                var moodHunger = MoodHungerLookup[entity];
                var isHungry = moodHunger.Hunger > 10;
                var hasAccessToBonfire = QuadrantSystem.TryFindEntity(QuadrantDataManager.BonfireQuadrantMap, GridManager, itemQuadrantsToSearch,
                    position, entity);

                var hasAccessToConstructable = QuadrantSystem.TryFindClosestEntity(QuadrantDataManager.ConstructableQuadrantMap, GridManager,
                    itemQuadrantsToSearch,
                    position,
                    entity, out var closestConstructable, out _);

                var hasAccessToStorageWithLogs = QuadrantSystem.TryFindNonEmptyStorageInSection(QuadrantDataManager.StorageQuadrantMap,
                    GridManager, itemQuadrantsToSearch, position, InventoryItem.LogOfWood);
                var hasAccessToStorageWithRawMeat = QuadrantSystem.TryFindNonEmptyStorageInSection(QuadrantDataManager.StorageQuadrantMap,
                    GridManager, itemQuadrantsToSearch, position, InventoryItem.RawMeat);
                var hasAccessToStorageWithCookedMeat = QuadrantSystem.TryFindNonEmptyStorageInSection(QuadrantDataManager.StorageQuadrantMap,
                    GridManager, itemQuadrantsToSearch, position, InventoryItem.CookedMeat);
                var hasAccessToStorageWithSpace = QuadrantSystem.TryFindSpaciousStorageInSection(QuadrantDataManager.StorageQuadrantMap,
                    GridManager, itemQuadrantsToSearch, position);
                var hasAccessToLogContainer = hasAccessToConstructable || hasAccessToStorageWithSpace;

                var hasAccessToDroppedLog = QuadrantSystem.TryFindClosestEntity(QuadrantDataManager.DroppedLogQuadrantMap, GridManager,
                    itemQuadrantsToSearch, position, entity, out _, out _);
                var hasAccessToDroppedRawMeat = QuadrantSystem.TryFindClosestEntity(QuadrantDataManager.DroppedRawMeatQuadrantMap, GridManager,
                    itemQuadrantsToSearch, position, entity, out _, out _);
                var hasAccessToDroppedCookedMeat = QuadrantSystem.TryFindClosestEntity(QuadrantDataManager.DroppedCookedMeatQuadrantMap, GridManager,
                    itemQuadrantsToSearch, position, entity, out _, out _);

                var isSleepy = moodSleepiness.Sleepiness > 0.2f;
                var isMoving = pathFollow.IsMoving();
                var isLonely = moodLoneliness.Loneliness > 10f;
                var hasInitiative = moodInitiative.HasInitiative();
                const float friendFactor = 1f;

                var socialRelationships = SocialRelationshipsLookup[entity];

                var isStandingOnNonWalkableCell = !GridManager.IsWalkable(cell) && !GridManager.IsBedAvailableToUnit(cell, entity);

                if (isStandingOnNonWalkableCell)
                {
                    EcbParallelWriter.AddComponent(i, entity, new IsIdle());
                    if (GridManager.TryGetNearbyEmptyCellSemiRandom(cell, out var nearbyCell, true, true))
                    {
                        EcbParallelWriter.AddComponent(i, entity, new Pathfinding
                        {
                            StartPosition = cell,
                            EndPosition = nearbyCell,
                            AllowNonWalkabes = true
                        });
                    }
                }
                else if (!isBaby && hasInitiative &&
                         QuadrantSystem.TryFindClosestEntity(QuadrantDataManager.BoarQuadrantMap, GridManager,
                             boarQuadrantsToSearch, position, entity,
                             out var nearbyBoar, out var distanceToBoar) &&
                         distanceToBoar < IsThrowingSpearSystem.Range)
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
                        MinTimeOfAction = ElapsedTime * TimeScale + randomDelay * TimeScale
                    });

                    if (HasItem(inventory))
                    {
                        InventoryHelpers.DropItemOnGround(EcbParallelWriter, i, ref inventory, position);
                    }
                }
                else if (HasLogOfWood(inventory))
                {
                    if (hasAccessToConstructable)
                    {
                        EcbParallelWriter.AddComponent(i, entity, new IsSeekingConstructable());
                    }
                    else if (hasAccessToStorageWithSpace)
                    {
                        EcbParallelWriter.AddComponent(i, entity, new IsSeekingRoomyStorage());
                    }
                    else
                    {
                        InventoryHelpers.DropItemOnGround(EcbParallelWriter, i, ref inventory, position);
                        EcbParallelWriter.AddComponent(i, entity, new IsIdle());
                    }
                }
                else if (HasCookedMeat(inventory) && isHungry)
                {
                    EcbParallelWriter.AddComponent(i, entity, new IsEatingMeat());
                    EcbParallelWriter.SetComponent(i, entity, new InventoryState
                    {
                        CurrentItem = InventoryItem.CookedMeat,
                        CurrentDurability = 1
                    });
                }
                else if (!isBaby && HasRawMeat(inventory) && isHungry && hasAccessToBonfire)
                {
                    if (IsAdjacentToBonfire(GridManager, cell, out _))
                    {
                        EcbParallelWriter.AddComponent(i, entity, new IsCookingMeat());
                        EcbParallelWriter.SetComponent(i, entity, new InventoryState
                        {
                            CurrentItem = InventoryItem.None,
                            CurrentDurability = 0
                        });
                    }
                    else
                    {
                        EcbParallelWriter.AddComponent(i, entity, new IsSeekingBonfire());
                    }
                }
                else if (HasItem(inventory))
                {
                    if (hasAccessToStorageWithSpace)
                    {
                        EcbParallelWriter.AddComponent(i, entity, new IsSeekingRoomyStorage());
                    }
                    else
                    {
                        InventoryHelpers.DropItemOnGround(EcbParallelWriter, i, ref inventory, position);
                        EcbParallelWriter.AddComponent(i, entity, new IsIdle());
                    }
                }
                else if (!isBaby && hasAccessToLogContainer && hasAccessToDroppedLog)
                {
                    EcbParallelWriter.AddComponent(i, entity, new IsSeekingDroppedItem
                    {
                        ItemType = InventoryItem.LogOfWood
                    });
                }
                else if (hasAccessToDroppedCookedMeat && (hasAccessToStorageWithSpace || isHungry))
                {
                    EcbParallelWriter.AddComponent(i, entity, new IsSeekingDroppedItem
                    {
                        ItemType = InventoryItem.CookedMeat
                    });
                }
                else if (!isBaby && hasAccessToDroppedRawMeat && (hasAccessToStorageWithSpace || (hasAccessToBonfire && isHungry)))
                {
                    EcbParallelWriter.AddComponent(i, entity, new IsSeekingDroppedItem
                    {
                        ItemType = InventoryItem.RawMeat
                    });
                }
                else if (isMoving)
                {
                    EcbParallelWriter.AddComponent(i, entity, new IsIdle());
                }
                else if (IsAnnoyedAtSomeone(
                             entity,
                             socialRelationships.Relationships,
                             SocialDynamicsManager.ThresholdForBecomingAnnoying,
                             position,
                             QuadrantDataManager,
                             out var annoyingDude))
                {
                    var annoyingDudePosition = LocalTransformLookup[annoyingDude].Position;
                    var distanceToTarget = math.distance(position, annoyingDudePosition);
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
                else if (isSleepy && GridManager.IsBedAvailableToUnit(position, entity))
                {
                    EcbParallelWriter.AddComponent(i, entity, new IsSleeping());
                }
                else if (isSleepy && hasInitiative)
                {
                    EcbParallelWriter.AddComponent(i, entity, new IsSeekingBed());
                }
                else if (!isBaby && hasAccessToLogContainer && IsAdjacentToTree(GridManager, cell, out var tree))
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
                             QuadrantDataManager.VillagerQuadrantMap, GridManager,
                             friendQuadrantsToSearch, position, entity, out _, out _)))
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
                else if (isHungry && hasAccessToStorageWithCookedMeat)
                {
                    EcbParallelWriter.AddComponent(i, entity, new IsSeekingFilledStorage
                    {
                        ItemType = InventoryItem.CookedMeat
                    });
                }
                else if (isHungry && hasAccessToStorageWithRawMeat && hasAccessToBonfire)
                {
                    EcbParallelWriter.AddComponent(i, entity, new IsSeekingFilledStorage
                    {
                        ItemType = InventoryItem.RawMeat
                    });
                }
                else if (!isBaby && hasAccessToConstructable && hasAccessToStorageWithLogs)
                {
                    EcbParallelWriter.AddComponent(i, entity, new IsSeekingFilledStorage
                    {
                        ItemType = InventoryItem.LogOfWood
                    });
                }
                else if (!isBaby && hasInitiative && hasAccessToLogContainer)
                {
                    EcbParallelWriter.AddComponent(i, entity, new IsSeekingTree());
                }
                else
                {
                    EcbParallelWriter.AddComponent<IsIdle>(i, entity);
                }
            }
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
            var quadrant = QuadrantSystem.GetHashMapKeyFromPosition(position);
            if (!quadrantDataManager.VillagerQuadrantMap.TryGetFirstValue(quadrant, out var data, out var iterator))
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
                     quadrantDataManager.VillagerQuadrantMap.TryGetNextValue(out data, ref iterator));

            annoyingDude = Entity.Null;
            return false;
        }

        private static bool HasItem(InventoryState inventory)
        {
            return inventory.CurrentItem != InventoryItem.None;
        }

        private static bool HasLogOfWood(InventoryState inventory)
        {
            return inventory.CurrentItem == InventoryItem.LogOfWood;
        }

        private static bool HasRawMeat(InventoryState inventory)
        {
            return inventory.CurrentItem == InventoryItem.RawMeat;
        }

        private static bool HasCookedMeat(InventoryState inventory)
        {
            return inventory.CurrentItem == InventoryItem.CookedMeat;
        }

        private static bool IsAdjacentToTree(GridManager gridManager, int2 cell, out int2 tree)
        {
            var foundTree = gridManager.TryGetNeighbouringTreeCell(cell, out tree);
            return foundTree;
        }

        private static bool IsAdjacentToBonfire(GridManager gridManager, int2 cell, out int2 bonfire)
        {
            return gridManager.TryGetNeighbouringBonfireCell(cell, out bonfire);
        }
    }
}
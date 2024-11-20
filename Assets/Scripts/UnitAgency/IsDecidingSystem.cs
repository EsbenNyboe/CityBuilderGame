using UnitBehaviours.AutonomousHarvesting;
using UnitBehaviours.Pathing;
using UnitBehaviours.Sleeping;
using UnitBehaviours.Targeting;
using UnitState;
using Unity.Burst;
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

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AttackAnimationManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
            _query = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<IsDeciding>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _attackAnimationManager = SystemAPI.GetSingleton<AttackAnimationManager>();
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, pathFollow, inventory, moodSleepiness, socialRelationships, entity)
                     in SystemAPI
                         .Query<RefRO<IsDeciding>, RefRO<PathFollow>, RefRO<Inventory>, RefRO<MoodSleepiness>,
                             RefRO<SocialRelationships>>()
                         .WithEntityAccess().WithNone<Pathfinding>())
            {
                ecb.RemoveComponent<IsDeciding>(entity);
                DecideNextBehaviour(ref state, gridManager, ecb, pathFollow, inventory, moodSleepiness,
                    socialRelationships, entity);
            }
        }

        private void DecideNextBehaviour(ref SystemState state,
            GridManager gridManager,
            EntityCommandBuffer ecb,
            RefRO<PathFollow> pathFollow,
            RefRO<Inventory> inventory,
            RefRO<MoodSleepiness> moodSleepiness,
            RefRO<SocialRelationships> socialRelationships,
            Entity entity)
        {
            var unitPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;
            var cell = GridHelpers.GetXY(unitPosition);

            var isSleepy = moodSleepiness.ValueRO.Sleepiness > 0.2f;
            var isMoving = pathFollow.ValueRO.IsMoving();
            var annoyingDude = socialRelationships.ValueRO.AnnoyingDude;
            var isAnnoyedAtSomeone = annoyingDude != Entity.Null;

            if (HasLogOfWood(inventory.ValueRO))
            {
                ecb.AddComponent(entity, new IsSeekingDropPoint());
            }
            else if (isMoving)
            {
                ecb.AddComponent(entity, new IsIdle());
            }
            else if (isAnnoyedAtSomeone)
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
            else
            {
                ecb.AddComponent(entity, new IsSeekingTree());
            }
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
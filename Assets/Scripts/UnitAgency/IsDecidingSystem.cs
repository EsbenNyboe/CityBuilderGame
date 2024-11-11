using UnitBehaviours.Animosity;
using UnitBehaviours.AutonomousHarvesting;
using UnitState;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitAgency
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup), OrderLast = true)]
    internal partial struct IsDecidingSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;
        private const double TimeOutBetweenSeekingDecisions = 1;
        private const float TimeToPlanMurder = 2f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Following the example at: https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-entity-command-buffer-automatic-playback.html
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var commands = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (_, inventory, moodSleepiness, pathFollow, socialRelationships, entity)
                     in SystemAPI
                         .Query<RefRO<IsDeciding>, RefRO<Inventory>, RefRW<MoodSleepiness>, RefRW<PathFollow>,
                             RefRO<SocialRelationships>>()
                         .WithEntityAccess())
            {
                commands.RemoveComponent<IsDeciding>(entity);
                DecideNextBehaviour(ref state, ref gridManager, commands, inventory, moodSleepiness, pathFollow,
                    socialRelationships, entity);
            }

            commands.Playback(state.EntityManager);
        }

        private void DecideNextBehaviour(ref SystemState state,
            ref GridManager gridManager,
            EntityCommandBuffer commands,
            RefRO<Inventory> inventory,
            RefRW<MoodSleepiness> moodSleepiness,
            RefRW<PathFollow> pathFollow,
            RefRO<SocialRelationships> socialRelationships,
            Entity entity)
        {
            var unitPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;
            var cell = GridHelpers.GetXY(unitPosition);
            var isSleepy = moodSleepiness.ValueRO.Sleepiness > 0.2f;
            var wasMoving = pathFollow.ValueRO.IsMoving();

            if (wasMoving && !gridManager.IsOccupied(cell, entity))
            {
                pathFollow.ValueRW.PathIndex = -1;
                gridManager.SetOccupant(cell, entity);
            }

            if (HasLogOfWood(inventory.ValueRO))
            {
                commands.AddComponent(entity, new IsSeekingDropPoint());
            }
            else if (socialRelationships.ValueRO.HasAnimosity &&
                     socialRelationships.ValueRO.TimeSinceAnimosityStarted > TimeToPlanMurder)
            {
                if (TryStartMurderingNeighbour(ref state, gridManager, socialRelationships, cell,
                        out var hatedNeighbour))
                {
                    commands.AddComponent(entity, new IsMurdering { Victim = hatedNeighbour });
                }
                else
                {
                    commands.AddComponent(entity, new IsSeekingVictim());
                }
            }
            else if (isSleepy && gridManager.IsBedAvailableToUnit(unitPosition, entity))
            {
                commands.AddComponent(entity, new IsSleeping());
                gridManager.SetIsWalkable(unitPosition, false);
                if (wasMoving)
                {
                    DebugHelper.LogError("Entity was moving while trying to sleep. Should that be possible?");
                }
            }
            else if (isSleepy && SystemAPI.Time.ElapsedTime - moodSleepiness.ValueRO.MostRecentSleepAction >
                     TimeOutBetweenSeekingDecisions)
            {
                moodSleepiness.ValueRW.MostRecentSleepAction = SystemAPI.Time.ElapsedTime;
                commands.AddComponent(entity, new IsSeekingBed());
            }
            else if (IsAdjacentToTree(ref state, gridManager, unitPosition, out var tree))
            {
                commands.AddComponent(entity, new IsHarvesting(tree));
                commands.AddComponent<ChopAnimationTag>(entity);
            }
            else
            {
                commands.AddComponent(entity, new IsSeekingTree());
            }

            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
        }

        private bool TryStartMurderingNeighbour(ref SystemState state, GridManager gridManager,
            RefRO<SocialRelationships> socialRelationships, int2 cell, out Entity hatedNeighbour)
        {
            // Look for neighbouring unit, who's standing still
            for (var i = 0; i < 8; i++)
            {
                if (gridManager.TryGetNeighbourCell(i, cell, out var neighbourCell) &&
                    gridManager.TryGetOccupant(neighbourCell, out var neighbourEntity))
                {
                    if (socialRelationships.ValueRO.Relationships[neighbourEntity] < -1f)
                    {
                        hatedNeighbour = neighbourEntity;
                        return true;
                    }
                }
            }

            // Look for neighbouring unit, who's moving
            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<SocialRelationships>().WithEntityAccess())
            {
                if (socialRelationships.ValueRO.Relationships[entity] < -1f &&
                    math.distance(cell, GridHelpers.GetXY(localTransform.ValueRO.Position)) < 2f)
                {
                    hatedNeighbour = entity;
                    return true;
                }
            }

            hatedNeighbour = Entity.Null;
            return false;
        }

        private bool HasLogOfWood(Inventory inventory)
        {
            return inventory.CurrentItem == InventoryItem.LogOfWood;
        }

        private bool IsAdjacentToTree(ref SystemState state, GridManager gridManager, float3 unitPosition,
            out int2 tree)
        {
            var cell = GridHelpers.GetXY(unitPosition);
            var foundTree = gridManager.TryGetNeighbouringTreeCell(cell, out tree);
            return foundTree;
        }
    }
}
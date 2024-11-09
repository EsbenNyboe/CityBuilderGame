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
            foreach (var (_, inventory, moodSleepiness, entity)
                     in SystemAPI.Query<RefRO<IsDeciding>, RefRO<Inventory>, RefRO<MoodSleepiness>>()
                         .WithEntityAccess())
            {
                commands.RemoveComponent<IsDeciding>(entity);
                DecideNextBehaviour(ref state, ref gridManager, commands, inventory, moodSleepiness, entity);
            }

            commands.Playback(state.EntityManager);
        }

        private void DecideNextBehaviour(ref SystemState state,
            ref GridManager gridManager,
            EntityCommandBuffer commands,
            RefRO<Inventory> inventory,
            RefRO<MoodSleepiness> moodSleepiness,
            Entity entity
        )
        {
            var unitPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;

            var isSleepy = moodSleepiness.ValueRO.Sleepiness > 0.2f;

            if (HasLogOfWood(inventory.ValueRO))
            {
                commands.AddComponent(entity, new IsSeekingDropPoint());
            }
            else if (isSleepy)
            {
                if (gridManager.IsBedAvailableToUnit(unitPosition, entity))
                {
                    commands.AddComponent(entity, new IsSleeping());
                    gridManager.SetIsWalkable(unitPosition, false);
                }
                else
                {
                    commands.AddComponent(entity, new IsSeekingBed());
                }
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
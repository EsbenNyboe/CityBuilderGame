using UnitBehaviours.AutonomousHarvesting;
using UnitBehaviours.Sleeping;
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
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
            _query = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<IsDeciding>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Following the example at: https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-entity-command-buffer-automatic-playback.html
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, inventory, moodSleepiness, entity)
                     in SystemAPI.Query<RefRO<IsDeciding>, RefRO<Inventory>, RefRO<MoodSleepiness>>()
                         .WithEntityAccess())
            {
                ecb.RemoveComponent<IsDeciding>(entity);
                DecideNextBehaviour(ref state, gridManager, ecb, inventory, moodSleepiness, entity);
            }
        }

        private void DecideNextBehaviour(ref SystemState state,
            GridManager gridManager,
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
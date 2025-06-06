using Grid;
using Inventory;
using UnitAgency.Data;
using UnitBehaviours.Pathing;
using UnitBehaviours.Targeting.Core;
using UnitBehaviours.UnitManagers;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    public partial struct IsSeekingDroppedLogSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<QuadrantDataManager>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var unitBehaviourManager = SystemAPI.GetSingleton<UnitBehaviourManager>();
            var quadrantDataManager = SystemAPI.GetSingleton<QuadrantDataManager>();
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (isSeekingDroppedLog, localTransform, pathFollow, inventory, entity) in
                     SystemAPI.Query<RefRW<IsSeekingDroppedLog>, RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<InventoryState>>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                var position = localTransform.ValueRO.Position;
                if (isSeekingDroppedLog.ValueRO.HasStartedMoving)
                {
                    if (QuadrantSystem.TryFindClosestEntity(quadrantDataManager.DroppedItemQuadrantMap, gridManager,
                            unitBehaviourManager.QuadrantSearchRange, position,
                            entity, out var droppedItemToPickup, out _))
                    {
                        var itemPosition = SystemAPI.GetComponent<LocalTransform>(droppedItemToPickup).Position;
                        if (math.distance(position, itemPosition) < 1 && inventory.ValueRO.CurrentItem == InventoryItem.None)
                        {
                            // PICK UP ITEM!
                            var droppedItem = SystemAPI.GetComponent<DroppedItem>(droppedItemToPickup);
                            inventory.ValueRW.CurrentItem = droppedItem.Item;
                            ecb.DestroyEntity(droppedItemToPickup);
                        }
                    }

                    ecb.RemoveComponent<IsSeekingDroppedLog>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                isSeekingDroppedLog.ValueRW.HasStartedMoving = true;

                if (QuadrantSystem.TryFindClosestEntity(quadrantDataManager.DroppedItemQuadrantMap, gridManager, 9, position,
                        entity, out var droppedItemToSeek, out _))
                {
                    PathHelpers.TrySetPath(ecb, gridManager, entity, GridHelpers.GetXY(position),
                        GridHelpers.GetXYRounded(SystemAPI.GetComponent<LocalTransform>(droppedItemToSeek).Position));
                }
            }
        }
    }
}
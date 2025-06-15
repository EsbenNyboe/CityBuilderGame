using System;
using Grid;
using Inventory;
using UnitAgency.Data;
using UnitBehaviours.AutonomousHarvesting.Model;
using UnitBehaviours.Pathing;
using UnitBehaviours.Targeting.Core;
using UnitBehaviours.UnitManagers;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    public partial struct IsSeekingDroppedItemSystem : ISystem
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
            foreach (var (isSeekingDroppedItem, localTransform, pathFollow, inventory, entity) in
                     SystemAPI.Query<RefRW<IsSeekingDroppedItem>, RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<InventoryState>>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                if (inventory.ValueRO.CurrentItem != InventoryItem.None)
                {
                    // I have picked up the dropped item.
                    ecb.RemoveComponent<IsSeekingDroppedItem>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                var position = localTransform.ValueRO.Position;
                // TODO: Remove this part? Not necessary, right?
                if (isSeekingDroppedItem.ValueRO.ItemType == InventoryItem.None)
                {
                    if (QuadrantSystem.TryFindClosestEntity(quadrantDataManager.DroppedCookedMeatQuadrantMap, gridManager,
                            unitBehaviourManager.QuadrantSearchRange, position,
                            entity, out _, out _))
                    {
                        isSeekingDroppedItem.ValueRW.ItemType = InventoryItem.CookedMeat;
                    }
                    else if (QuadrantSystem.TryFindClosestEntity(quadrantDataManager.DroppedRawMeatQuadrantMap, gridManager,
                                 unitBehaviourManager.QuadrantSearchRange, position,
                                 entity, out _, out _))
                    {
                        isSeekingDroppedItem.ValueRW.ItemType = InventoryItem.RawMeat;
                    }
                    else if (QuadrantSystem.TryFindClosestEntity(quadrantDataManager.DroppedLogQuadrantMap, gridManager,
                                 unitBehaviourManager.QuadrantSearchRange, position,
                                 entity, out _, out _))
                    {
                        isSeekingDroppedItem.ValueRW.ItemType = InventoryItem.LogOfWood;
                    }
                    else
                    {
                        // I can't see any dropped items nearby.
                        ecb.RemoveComponent<IsSeekingDroppedItem>(entity);
                        ecb.AddComponent<IsDeciding>(entity);
                        continue;
                    }
                }

                var droppedItemQuadrantMap = isSeekingDroppedItem.ValueRO.ItemType switch
                {
                    InventoryItem.None => throw new ArgumentOutOfRangeException(),
                    InventoryItem.LogOfWood => quadrantDataManager.DroppedLogQuadrantMap,
                    InventoryItem.RawMeat =>  quadrantDataManager.DroppedRawMeatQuadrantMap,
                    InventoryItem.CookedMeat => quadrantDataManager.DroppedCookedMeatQuadrantMap,
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (isSeekingDroppedItem.ValueRO.HasStartedMoving)
                {
                    if (QuadrantSystem.TryFindClosestEntity(droppedItemQuadrantMap, gridManager,
                            unitBehaviourManager.QuadrantSearchRange, position,
                            entity, out var droppedItemToPickup, out _))
                    {
                        var itemPosition = SystemAPI.GetComponent<LocalTransform>(droppedItemToPickup).Position;
                        if (math.distance(position, itemPosition) < 1)
                        {
                            ecb.AddComponent(ecb.CreateEntity(), new DroppedItemRequest
                            {
                                RequesterEntity = entity,
                                DroppedItemEntity = droppedItemToPickup
                            });
                        }
                    }

                    ecb.RemoveComponent<IsSeekingDroppedItem>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                isSeekingDroppedItem.ValueRW.HasStartedMoving = true;

                if (QuadrantSystem.TryFindClosestEntity(droppedItemQuadrantMap, gridManager, 9, position,
                        entity, out var droppedItemToSeek, out _))
                {
                    PathHelpers.TrySetPath(ecb, gridManager, entity, GridHelpers.GetXY(position),
                        GridHelpers.GetXYRounded(SystemAPI.GetComponent<LocalTransform>(droppedItemToSeek).Position));
                }
            }
        }
    }
}
using Debugging;
using Grid;
using Inventory;
using SystemGroups;
using UnitAgency.Data;
using UnitBehaviours.Pathing;
using UnitBehaviours.Targeting.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsSeekingRoomyStorageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<QuadrantDataManager>();
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<DebugToggleManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var quadrantDataManager = SystemAPI.GetSingleton<QuadrantDataManager>();
            var isDebugging = SystemAPI.GetSingleton<DebugToggleManager>().DebugPathfinding;
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Seek Storage
            foreach (var (localTransform, pathFollow, inventory, entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<InventoryState>>()
                         .WithAll<IsSeekingRoomyStorage>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                if (inventory.ValueRO.CurrentItem == InventoryItem.None)
                {
                    ecb.RemoveComponent<IsSeekingRoomyStorage>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                var position = localTransform.ValueRO.Position;
                var cell = GridHelpers.GetXY(position);
                var closestStorageEntrance =
                    FindClosestStorageEntrance(ref state, quadrantDataManager, gridManager, position, out var closestStorageCell);
                if (cell.Equals(closestStorageEntrance))
                {
                    // Try drop item at drop point
                    InventoryHelpers.SendRequestForStoreItem(ecb, entity, closestStorageCell, inventory.ValueRO.CurrentItem);
                    continue;
                }

                if (closestStorageEntrance.x > -1)
                {
                    PathHelpers.TrySetPath(ecb, gridManager, entity, cell, closestStorageEntrance, isDebugging);
                }
                else
                {
                    // Drop item on ground
                    InventoryHelpers.DropItemOnGround(ecb, ref inventory.ValueRW, position);
                    ecb.RemoveComponent<IsSeekingRoomyStorage>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }
        }

        private int2 FindClosestStorageEntrance(ref SystemState state, QuadrantDataManager quadrantDataManager, GridManager gridManager,
            float3 position, out int2 closestStorageCell)
        {
            closestStorageCell = new int2(-1);
            var cell = GridHelpers.GetXY(position);

            if (!QuadrantSystem.TryFindClosestSpaciousStorage(quadrantDataManager.StorageQuadrantMap, gridManager, 50, position,
                    out var closestStorage))
            {
                return -1;
            }

            closestStorageCell = GridHelpers.GetXY(SystemAPI.GetComponent<LocalTransform>(closestStorage.Entity).Position);

            if (!gridManager.TryGetClosestWalkableNeighbourOfTarget(cell, closestStorageCell, out var closestStorageEntrance))
            {
                return -1;
            }

            return closestStorageEntrance;
        }
    }
}
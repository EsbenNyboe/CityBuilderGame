using Debugging;
using Grid;
using Inventory;
using SystemGroups;
using UnitAgency.Data;
using UnitBehaviours.Pathing;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsSeekingDropPointSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<DebugToggleManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var isDebugging = SystemAPI.GetSingleton<DebugToggleManager>().DebugPathfinding;
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Seek DropPoint
            foreach (var (localTransform, pathFollow, inventory, entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<InventoryState>>()
                         .WithAll<IsSeekingDropPoint>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                if (inventory.ValueRO.CurrentItem == InventoryItem.None)
                {
                    ecb.RemoveComponent<IsSeekingDropPoint>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                var position = localTransform.ValueRO.Position;
                var cell = GridHelpers.GetXY(position);
                var closestDropPointEntrance = FindClosestDropPointEntrance(ref state, gridManager, position, out var closestDropPointCell);
                if (cell.Equals(closestDropPointEntrance))
                {
                    // Try drop item at drop point
                    InventoryHelpers.SendRequestForDropItem(ecb, entity, closestDropPointCell);
                    continue;
                }

                if (closestDropPointEntrance.x > -1)
                {
                    PathHelpers.TrySetPath(ecb, gridManager, entity, cell, closestDropPointEntrance, isDebugging);
                }
                else
                {
                    // Drop item on ground
                    InventoryHelpers.DropItemOnGround(ecb, ref inventory.ValueRW, position);
                    ecb.RemoveComponent<IsSeekingDropPoint>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }
        }

        private int2 FindClosestDropPointEntrance(ref SystemState state, GridManager gridManager, float3 position, out int2 closestDropPointCell)
        {
            closestDropPointCell = new int2(-1);
            var closestDropPointEntrance = new int2(-1);
            var shortestDropPointDistance = math.INFINITY;
            var cell = GridHelpers.GetXY(position);

            foreach (var (dropPointTransform, dropPoint) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<DropPoint>>())
            {
                var dropPointPosition = dropPointTransform.ValueRO.Position;
                var dropPointCell = GridHelpers.GetXY(dropPointPosition);

                var itemCount = gridManager.GetStorageItemCount(dropPointCell);
                var itemCapacity = gridManager.GetStorageItemCapacity(dropPointCell);

                if (gridManager.GetStorageItemCount(dropPointCell) >= gridManager.GetStorageItemCapacity(dropPointCell))
                {
                    continue;
                }

                var dropPointDistance = math.distance(position, dropPointPosition);
                if (dropPointDistance < shortestDropPointDistance &&
                    gridManager.TryGetClosestWalkableNeighbourOfTarget(cell, dropPointCell, out var dropPointEntrance))
                {
                    shortestDropPointDistance = dropPointDistance;
                    closestDropPointCell = dropPointCell;
                    closestDropPointEntrance = dropPointEntrance;
                }
            }

            return closestDropPointEntrance;
        }
    }
}
using Debugging;
using Grid;
using GridEntityNS;
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
    public partial struct IsSeekingConstructableSystem : ISystem
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

            // Seek Constructable
            foreach (var (localTransform, pathFollow, inventory, entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<InventoryState>>()
                         .WithAll<IsSeekingConstructable>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                if (inventory.ValueRO.CurrentItem == InventoryItem.None)
                {
                    ecb.RemoveComponent<IsSeekingConstructable>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                var position = localTransform.ValueRO.Position;
                var cell = GridHelpers.GetXY(position);
                var closestConstructableEntrance =
                    FindClosestConstructableEntrance(ref state, gridManager, position, out var closestConstructableCell);
                if (cell.Equals(closestConstructableEntrance))
                {
                    // Try drop item at drop point
                    InventoryHelpers.SendRequestForConstructItem(ecb, entity, closestConstructableCell);
                    continue;
                }

                if (closestConstructableEntrance.x > -1)
                {
                    PathHelpers.TrySetPath(ecb, gridManager, entity, cell, closestConstructableEntrance, isDebugging);
                }
                else
                {
                    // Drop item on ground
                    InventoryHelpers.DropItemOnGround(ecb, ref inventory.ValueRW, position);
                    ecb.RemoveComponent<IsSeekingConstructable>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }
        }

        private int2 FindClosestConstructableEntrance(ref SystemState state, GridManager gridManager, float3 position,
            out int2 closestConstructableCell)
        {
            closestConstructableCell = new int2(-1);
            var closestConstructableEntrance = new int2(-1);
            var shortestConstructableDistance = math.INFINITY;
            var cell = GridHelpers.GetXY(position);

            foreach (var (constructableTransform, constructable) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Constructable>>())
            {
                var constructablePosition = constructableTransform.ValueRO.Position;
                var constructableCell = GridHelpers.GetXY(constructablePosition);

                if (gridManager.GetStorageItemCount(constructableCell) >= gridManager.GetStorageItemCapacity(constructableCell))
                {
                    continue;
                }

                var constructableDistance = math.distance(position, constructablePosition);
                if (constructableDistance < shortestConstructableDistance &&
                    gridManager.TryGetClosestWalkableNeighbourOfTarget(cell, constructableCell, out var constructableEntrance))
                {
                    shortestConstructableDistance = constructableDistance;
                    closestConstructableCell = constructableCell;
                    closestConstructableEntrance = constructableEntrance;
                }
            }

            return closestConstructableEntrance;
        }
    }
}
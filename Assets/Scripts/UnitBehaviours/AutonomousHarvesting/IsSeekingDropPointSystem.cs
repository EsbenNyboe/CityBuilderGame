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
    public partial struct IsSeekingDropPointSystem : ISystem
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
                var closestDropPointEntrance =
                    FindClosestDropPointEntrance(ref state, quadrantDataManager, gridManager, position, out var closestDropPointCell);
                if (cell.Equals(closestDropPointEntrance))
                {
                    // Try drop item at drop point
                    InventoryHelpers.SendRequestForStoreItem(ecb, entity, closestDropPointCell);
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

        private int2 FindClosestDropPointEntrance(ref SystemState state, QuadrantDataManager quadrantDataManager, GridManager gridManager,
            float3 position, out int2 closestDropPointCell)
        {
            closestDropPointCell = new int2(-1);
            var cell = GridHelpers.GetXY(position);

            if (!QuadrantSystem.TryFindClosestSpaciousStorage(quadrantDataManager.DropPointQuadrantMap, gridManager, 50, position,
                    out var closestDropPointEntity))
            {
                return -1;
            }

            closestDropPointCell = GridHelpers.GetXY(SystemAPI.GetComponent<LocalTransform>(closestDropPointEntity).Position);

            if (!gridManager.TryGetClosestWalkableNeighbourOfTarget(cell, closestDropPointCell, out var closestDropPointEntrance))
            {
                return -1;
            }

            return closestDropPointEntrance;
        }
    }
}
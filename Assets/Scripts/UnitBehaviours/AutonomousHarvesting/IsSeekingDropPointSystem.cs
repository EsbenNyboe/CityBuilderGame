using Debugging;
using Inventory;
using SystemGroups;
using UnitAgency;
using UnitBehaviours.Pathing;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    public struct IsSeekingDropPoint : IComponentData
    {
    }

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
            foreach (var (localTransform, pathFollow, inventory, entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<InventoryState>>()
                         .WithAll<IsSeekingDropPoint>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                var position = localTransform.ValueRO.Position;
                var cell = GridHelpers.GetXY(position);
                var closestDropPointEntrance = FindClosestDropPointEntrance(ref state, gridManager, position);
                if (cell.Equals(closestDropPointEntrance))
                {
                    // Drop item at drop point
                    inventory.ValueRW.CurrentItem = InventoryItem.None;
                    ecb.RemoveComponent<IsSeekingDropPoint>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
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

        private int2 FindClosestDropPointEntrance(ref SystemState state, GridManager gridManager, float3 position)
        {
            var closestDropPointEntrance = new int2(-1);
            var shortestDropPointDistance = math.INFINITY;
            var cell = GridHelpers.GetXY(position);

            foreach (var (dropPointTransform, dropPoint) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<DropPoint>>())
            {
                var dropPointPosition = dropPointTransform.ValueRO.Position;
                var dropPointCell = GridHelpers.GetXY(dropPointPosition);
                var dropPointDistance = math.distance(position, dropPointPosition);
                if (dropPointDistance < shortestDropPointDistance &&
                    gridManager.TryGetClosestWalkableNeighbourOfTarget(cell, dropPointCell, out var dropPointEntrance))
                {
                    shortestDropPointDistance = dropPointDistance;
                    closestDropPointEntrance = dropPointEntrance;
                }
            }

            return closestDropPointEntrance;
        }
    }
}
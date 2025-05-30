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
    public partial struct IsSeekingConstructableSystem : ISystem
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

                // TODO: Extract "quadrantsToSearch" to global value.
                int2 closestConstructableEntrance = -1;
                int2 constructableCell = -1;
                if (QuadrantSystem.TryFindClosestEntity(quadrantDataManager.ConstructableQuadrantMap, gridManager, 50,
                        position, entity,
                        out var closestConstructable, out _))
                {
                    constructableCell = GridHelpers.GetXY(SystemAPI.GetComponent<LocalTransform>(closestConstructable).Position);
                    gridManager.TryGetClosestWalkableNeighbourOfTarget(cell, constructableCell, out closestConstructableEntrance);
                }

                if (cell.Equals(closestConstructableEntrance))
                {
                    // Try drop item at constructable
                    InventoryHelpers.SendRequestForConstructItem(ecb, entity, constructableCell);
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
    }
}
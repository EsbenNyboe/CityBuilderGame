using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Grid
{
    [UpdateInGroup(typeof(UnitBehaviourGridWritingSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(GridManagerSectionSortingSystem))]
    public partial struct PathfindingInvalidationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GridManager>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (pathfinding, entity) in SystemAPI.Query<RefRO<Pathfinding>>().WithEntityAccess())
            {
                if (!gridManager.IsWalkable(pathfinding.ValueRO.EndPosition))
                {
                    Debug.LogError("Not walkable");
                    ecb.RemoveComponent<Pathfinding>(entity);
                }

                if (!gridManager.IsMatchingSection(pathfinding.ValueRO.StartPosition, pathfinding.ValueRO.EndPosition))
                {
                    ecb.RemoveComponent<Pathfinding>(entity);
                }
            }
        }
    }
}
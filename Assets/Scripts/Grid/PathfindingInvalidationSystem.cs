using Unity.Burst;
using Unity.Entities;

namespace Grid
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct PathfindingInvalidationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GridManager>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (pathfinding, entity) in SystemAPI.Query<RefRO<Pathfinding>>().WithEntityAccess())
            {
                if (!gridManager.IsWalkable(pathfinding.ValueRO.EndPosition))
                {
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
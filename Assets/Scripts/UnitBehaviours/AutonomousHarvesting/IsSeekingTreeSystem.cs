using UnitAgency;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    [BurstCompile]
    public partial struct IsSeekingTreeSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (localTransform, pathFollow, entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathFollow>>().WithAll<IsSeekingTree>().WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }
                // I reacted my destination / I'm standing still: I should find a bed!

                // Am I adjacent to a tree?
                var unitPosition = localTransform.ValueRO.Position;
                GridHelpers.GetXY(unitPosition, out var x, out var y);

                if (gridManager.TryGetNeighbouringTreeCell(x, y, out _, out _))
                {
                    // I found my adjacent tree! 
                    ecb.RemoveComponent<IsSeekingTree>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                // I'm not next to a tree... I should find the closest tree.
                var currentCell = GridHelpers.GetXY(unitPosition);
                if (!gridManager.TryGetNearbyChoppingCell(currentCell, out _, out var choppingCell))
                {
                    // I can't see any nearby trees
                    ecb.RemoveComponent<IsSeekingTree>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                // I found a tree!! I will go there! 
                PathHelpers.TrySetPath(ecb, entity, currentCell, choppingCell);
            }

            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
            ecb.Playback(state.EntityManager);
        }
    }
}
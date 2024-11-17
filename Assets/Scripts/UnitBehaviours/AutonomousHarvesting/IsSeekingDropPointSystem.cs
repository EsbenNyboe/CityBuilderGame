using UnitAgency;
using UnitState;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    [BurstCompile]
    public partial struct IsSeekingDropPointSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (localTransform, pathFollow, inventory, entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<Inventory>>()
                         .WithAll<IsSeekingDropPoint>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                var unitWorldPosition = localTransform.ValueRO.Position;
                var unitGridPosition = GridHelpers.GetXY(unitWorldPosition);
                var closestDropPointEntrance =
                    FindClosestDropPoint(ref state, ref gridManager, unitWorldPosition, entity);
                if (unitGridPosition.Equals(closestDropPointEntrance))
                {
                    inventory.ValueRW.CurrentItem = InventoryItem.None;
                    ecb.RemoveComponent<IsSeekingDropPoint>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                if (closestDropPointEntrance.x > -1)
                {
                    PathHelpers.TrySetPath(ecb, entity, unitGridPosition, closestDropPointEntrance);
                }
            }

            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
        }

        private int2 FindClosestDropPoint(ref SystemState state,
            ref GridManager gridManager,
            float3 position, Entity selfEntity)
        {
            var closestDropPoint = new float3(-1);
            var shortestDropPointDistance = math.INFINITY;

            foreach (var (dropPointTransform, dropPoint) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<DropPoint>>())
            {
                var dropPointPosition = dropPointTransform.ValueRO.Position;
                var dropPointDistance = math.distance(position, dropPointPosition);
                if (dropPointDistance < shortestDropPointDistance)
                {
                    shortestDropPointDistance = dropPointDistance;
                    closestDropPoint = dropPointPosition;
                }
            }

            if (!(closestDropPoint.x > -1))
            {
                return -1;
            }

            var dropPointCell = GridHelpers.GetXY(closestDropPoint);
            var cellPosition = GridHelpers.GetXY(position);
            // TODO: Make unit select second-closest droppoint, if there's no path to the closest one.
            gridManager.TryGetClosestValidNeighbourOfTarget(cellPosition, selfEntity, dropPointCell,
                out var dropPointEntrance);

            return dropPointEntrance;
        }
    }
}
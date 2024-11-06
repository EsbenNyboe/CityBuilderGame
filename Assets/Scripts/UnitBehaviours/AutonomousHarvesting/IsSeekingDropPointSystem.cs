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
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (localTransform, pathFollow, inventory, entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<Inventory>>().WithAll<IsSeekingDropPoint>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                var unitWorldPosition = localTransform.ValueRO.Position;
                var unitGridPosition = GridHelpers.GetXY(unitWorldPosition);
                var closestDropPointEntrance = FindClosestDropPoint(ref state, ref gridManager, unitWorldPosition);
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
            ecb.Playback(state.EntityManager);
        }

        private int2 FindClosestDropPoint(
            ref SystemState state,
            ref GridManager gridManager,
            float3 position
        )
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

            GridHelpers.GetXY(closestDropPoint, out var x, out var y);
            var dropPointCell = new int2(x, y);
            var closestDropPointEntrance = new int2(-1, -1);
            var shortestDropPointEntranceDistance = math.INFINITY;
            GridHelpers.GetXY(position, out var posX, out var posY);
            var cellPosition = new int2(posX, posY);
            gridManager.RandomizeNeighbourSequenceIndex();
            for (var i = 0; i < 8; i++)
            {
                gridManager.GetSequencedNeighbourCell(dropPointCell.x, dropPointCell.y, out var dropPointEntranceX,
                    out var dropPointEntranceY);
                var dropPointEntrance = new int2(dropPointEntranceX, dropPointEntranceY);
                var dropPointEntranceDistance = math.distance(cellPosition, dropPointEntrance);
                if (dropPointEntranceDistance < shortestDropPointEntranceDistance)
                {
                    closestDropPointEntrance = dropPointEntrance;
                    shortestDropPointEntranceDistance = dropPointEntranceDistance;
                }
            }

            return closestDropPointEntrance;
        }
    }
}
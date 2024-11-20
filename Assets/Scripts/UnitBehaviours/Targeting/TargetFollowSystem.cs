using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitBehaviours.Pathing
{
    public struct TargetFollow : IComponentData
    {
        public Entity Target;
        public float DesiredRange;
        public float CurrentDistanceToTarget;

        public readonly bool TryGetTarget(out Entity target)
        {
            target = Target;
            return target != Entity.Null;
        }
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct TargetFollowSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;
        private const bool IsDebugging = true;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem(typeof(GridManagerSystem));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            foreach (var (targetFollow, localTransform, pathFollow, entity) in
                     SystemAPI.Query<RefRW<TargetFollow>, RefRO<LocalTransform>, RefRO<PathFollow>>()
                         .WithEntityAccess())
            {
                if (!targetFollow.ValueRO.TryGetTarget(out var target))
                {
                    continue;
                }

                if (!transformLookup.HasComponent(target))
                {
                    DebugHelper.LogError("Target has no transform!");
                    continue;
                }

                var targetPosition = transformLookup[target].Position;
                var currentPosition = localTransform.ValueRO.Position;
                if (IsDebugging)
                {
                    Debug.DrawLine(currentPosition, targetPosition, Color.red);
                }

                // TODO: Make it able to update its path, while moving
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                var distanceToTarget = math.distance(currentPosition, targetPosition);
                targetFollow.ValueRW.CurrentDistanceToTarget = distanceToTarget;

                if (distanceToTarget > targetFollow.ValueRO.DesiredRange)
                {
                    var currentCell = GridHelpers.GetXY(currentPosition);
                    var targetCell = GridHelpers.GetXY(targetPosition);

                    if (gridManager.TryGetClosestValidNeighbourOfTarget(currentCell, entity, targetCell,
                            out var neighbourCell))
                    {
                        // I'll go stand next to my target. 
                        PathHelpers.TrySetPath(ecb, entity, currentCell, neighbourCell, IsDebugging);
                    }
                    else if (gridManager.TryGetNearbyEmptyCellSemiRandom(targetCell, out var nearbyCell) &&
                             math.distance(currentCell, targetCell) > math.distance(nearbyCell, targetCell))
                    {
                        // I'll move a bit closer to my target.
                        PathHelpers.TrySetPath(ecb, entity, currentCell, nearbyCell, IsDebugging);
                    }
                    else
                    {
                        // I can't move any closer to my target!
                        if (IsDebugging)
                        {
                            Debug.LogError("I can't move any closer to my target!");
                        }

                        // I give up...
                        targetFollow.ValueRW.Target = Entity.Null;
                        targetFollow.ValueRW.CurrentDistanceToTarget = math.INFINITY;
                        targetFollow.ValueRW.DesiredRange = 0;
                    }
                }
            }
        }
    }
}
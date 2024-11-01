using UnitAgency;
using Unity.Entities;
using Unity.Transforms;
using ISystem = Unity.Entities.ISystem;
using SystemAPI = Unity.Entities.SystemAPI;
using SystemHandle = Unity.Entities.SystemHandle;
using SystemState = Unity.Entities.SystemState;

[UpdateAfter(typeof(PathfindingSystem))]
[UpdateAfter(typeof(GridManagerSystem))]
public partial struct IsSeekingBedSystem : ISystem
{
    private SystemHandle _gridManagerSystemHandle;

    public void OnCreate(ref SystemState state)
    {
        _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        foreach (var (localTransform, pathFollow, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathFollow>>().WithAll<IsSeekingBed>()
                     .WithEntityAccess())
        {
            if (pathFollow.ValueRO.IsMoving())
            {
                continue;
            }
            // I reacted my destination / I'm standing still: I should find a bed!

            // Am I on a bed?
            var unitPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;
            if (gridManager.IsBed(unitPosition) && !gridManager.IsOccupied(unitPosition, entity))
            {
                // Ahhhh, I found my bed! 
                ecb.RemoveComponent<IsSeekingBed>(entity);
                ecb.AddComponent<IsDeciding>(entity);
                continue;
            }

            // I'm not on a bed... I should find the closest bed.
            var currentCell = GridHelpers.GetXY(unitPosition);
            if (!gridManager.TryGetClosestBedSemiRandom(currentCell, out var closestAvailableBed))
            {
                // There is no available bed anywhere!
                if (gridManager.IsInteractable(unitPosition))
                {
                    // Whoops, I'm standing on a bed.. I should move..
                    if (gridManager.TryGetNearbyEmptyCellSemiRandom(currentCell, out var nearbyCell))
                    {
                        PathHelpers.TrySetPath(ecb, entity, currentCell, nearbyCell);
                    }
                }

                // I guess I have to wait for a bed to be available...
                // I'll keep checking all beds every frame, until I succeed!!!!
                continue;
            }

            // I found a bed!! I will go there! 
            PathHelpers.TrySetPath(ecb, entity, currentCell, closestAvailableBed);
        }

        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
        ecb.Playback(state.EntityManager);
    }
}
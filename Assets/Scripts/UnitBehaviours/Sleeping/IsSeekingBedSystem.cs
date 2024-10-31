using UnitAgency;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ISystem = Unity.Entities.ISystem;
using SystemAPI = Unity.Entities.SystemAPI;
using SystemHandle = Unity.Entities.SystemHandle;
using SystemState = Unity.Entities.SystemState;

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
            if (gridManager.IsInteractable(unitPosition) && !gridManager.IsInteractedWith(unitPosition))
            {
                // Ahhhh, I found my bed! 
                ecb.RemoveComponent<IsSeekingBed>(entity);
                ecb.AddComponent<IsDeciding>(entity);
                continue;
            }

            // I'm not on a bed... I should find the closest bed.
            var closestBed = GetClosestBed(ref state, gridManager, unitPosition);

            if (closestBed.x < 0)
            {
                // There is no bed! 
                // TODO: At least make sure, I'm not on an occupied spot..
                continue;
            }

            // I found a bed!! I will go there! 
            GridHelpers.GetXY(unitPosition, out var startX, out var startY);
            GridHelpers.GetXY(closestBed, out var endX, out var endY);

            if (!PathHelpers.TrySetPath(ecb, entity, startX, startY, endX, endY))
            {
                Debug.Log("No need to set a path. Already at destination");
            }
        }

        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
        ecb.Playback(state.EntityManager);
    }

    private float3 GetClosestBed(ref SystemState state, GridManager gridManager, float3 unitPosition)
    {
        var closestBed = new float3(-1, -1, -1);
        var shortestDistance = Mathf.Infinity;
        foreach (var (bed, bedLocalTransform) in SystemAPI.Query<RefRO<Bed>, RefRO<LocalTransform>>())
        {
            var bedPosition = bedLocalTransform.ValueRO.Position;
            var distance = Vector3.Distance(unitPosition, bedPosition);
            if (distance < shortestDistance && !gridManager.IsInteractedWith(bedPosition) && distance > Mathf.Epsilon)
            {
                shortestDistance = distance;
                closestBed = bedPosition;
            }
        }

        return closestBed;
    }
}
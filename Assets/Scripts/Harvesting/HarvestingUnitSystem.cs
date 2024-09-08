using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class HarvestingUnitSystem : SystemBase
{
    protected override void OnUpdate()
    {
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);
        foreach (var (harvestingUnit, pathFollow, localTransform, entity) in SystemAPI
                     .Query<RefRW<HarvestingUnit>, RefRO<PathFollow>, RefRO<LocalTransform>>()
                     .WithAll<HarvestingUnit>().WithEntityAccess())
        {
            if (pathFollow.ValueRO.PathIndex >= 0)
            {
                continue;
            }

            var target = harvestingUnit.ValueRO.Target;
            var targetX = target.x;
            var targetY = target.y;

            if (PathfindingGridSetup.Instance.pathfindingGrid.GetGridObject(targetX, targetY).IsWalkable())
            {
                // Tree probably was destroyed, during pathfinding
                EntityManager.SetComponentEnabled<HarvestingUnit>(entity, false);
                harvestingUnit.ValueRW.IsHarvesting = false;
                harvestingUnit.ValueRW.Target = new int2(-1, -1);
            }

            if (!harvestingUnit.ValueRO.IsHarvesting)
            {
                harvestingUnit.ValueRW.IsHarvesting = true;

                // TODO: Replace this with trees as grid
                SetDegradationState(targetX, targetY, true);
            }
            else
            {
                PathfindingGridSetup.Instance.pathfindingGrid.GetXY(localTransform.ValueRO.Position, out var x,
                    out var y);
                if (Mathf.Abs(Mathf.Abs(targetX) - Mathf.Abs(x)) > 1 &&
                    Mathf.Abs(Mathf.Abs(targetY) - Mathf.Abs(y)) > 1)
                {
                    // Tree is too far away to harvest
                    // Check surroundings 
                    PathfindingGridSetup.Instance.pathfindingGrid.GetXY(localTransform.ValueRO.Position, out var startX,
                        out var startY);



                    //List<int2> movePositionList = GetCellListAroundTargetCell(target, 20);

                    //for (int i = 0; i < movePositionList.Count; i++)
                    //{
                    //    for (int j = i + 1; j < movePositionList.Count; j++)
                    //    {
                    //        if (movePositionList[i].x == movePositionList[j].x && movePositionList[i].y == movePositionList[j].y)
                    //        {
                    //            Debug.Log("Position list contains duplicate: " + movePositionList[i]);
                    //        }
                    //    }
                    //}

                    //if (IsPositionInsideGrid(target))
                    //{
                    //    if (IsPositionWalkable(target))
                    //    {
                    //    }
                    //    else
                    //    {
                    //    }
                    //}


                    var endPosition = new int2(x, y);

                    entityCommandBuffer.AddComponent(entity, new PathfindingParams
                    {
                        StartPosition = new int2(startX, startY),
                        EndPosition = endPosition
                    });
                }
            }
        }

        entityCommandBuffer.Playback(EntityManager);
    }

    private void SetDegradationState(int targetX, int targetY, bool state)
    {
        foreach (var (localTransform, unitDegradation) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<UnitDegradation>>())
        {
            PathfindingGridSetup.Instance.pathfindingGrid.GetXY(localTransform.ValueRO.Position, out var x, out var y);
            if (targetX != x || targetY != y)
            {
                continue;
            }

            unitDegradation.ValueRW.IsDegrading = state;
        }
    }

    private bool TryGetNearbyTree(int unitPosX, int unitPosY, out int treePosX, out int treePosY)
    {
        if (!NeighbourIsWalkable(unitPosX, unitPosY, 1, 0, out treePosX, out treePosY))
        {
            return true;
        }

        if (!NeighbourIsWalkable(unitPosX, unitPosY, 1, 1, out treePosX, out treePosY))
        {
            return true;
        }

        if (!NeighbourIsWalkable(unitPosX, unitPosY, 0, 1, out treePosX, out treePosY))
        {
            return true;
        }

        if (!NeighbourIsWalkable(unitPosX, unitPosY, -1, 1, out treePosX, out treePosY))
        {
            return true;
        }

        if (!NeighbourIsWalkable(unitPosX, unitPosY, -1, 0, out treePosX, out treePosY))
        {
            return true;
        }

        if (!NeighbourIsWalkable(unitPosX, unitPosY, -1, -1, out treePosX, out treePosY))
        {
            return true;
        }

        if (!NeighbourIsWalkable(unitPosX, unitPosY, 0, -1, out treePosX, out treePosY))
        {
            return true;
        }

        if (!NeighbourIsWalkable(unitPosX, unitPosY, 1, -1, out treePosX, out treePosY))
        {
            return true;
        }

        return false;
    }

    private bool NeighbourIsWalkable(int unitPosX, int unitPosY, int xAddition, int yAddition, out int treePosX,
        out int treePosY)
    {
        treePosX = unitPosX + xAddition;
        treePosY = unitPosY + yAddition;
        return PathfindingGridSetup.Instance.pathfindingGrid.GetGridObject(treePosX, treePosY).IsWalkable();
    }




    // TODO: Refactor duplicate code
    private List<int2> GetCellListAroundTargetCell(int2 firstPosition, int ringCount)
    {
        var positionList = new List<int2> { firstPosition };

        for (int i = 1; i < ringCount; i++)
        {
            for (int j = 1; j < i; j++)
            {
                AddFourPositionsAroundTarget(positionList, firstPosition, i, j);
                AddFourPositionsAroundTarget(positionList, firstPosition, j, i);
            }

            if (i - 1 > 0)
            {
                AddFourPositionsAroundTarget(positionList, firstPosition, i - 1, i - 1);
            }

            positionList.Add(firstPosition + new int2(i, 0));
            positionList.Add(firstPosition + new int2(-i, 0));
            positionList.Add(firstPosition + new int2(0, i));
            positionList.Add(firstPosition + new int2(0, -i));
        }

        return positionList;
    }

    private static void AddFourPositionsAroundTarget(List<int2> positionList, int2 firstPosition, int a, int b)
    {
        if (positionList.Contains(firstPosition + new int2(a, b)))
        {
            for (int i = 0; i < positionList.Count; i++)
            {
                if (positionList[i].Equals(firstPosition + new int2(a, b)))
                {
                    Debug.Log("DUPLICATE IS THIS: " + i);
                }
            }
            Debug.Log("Duplicate found on index: " + positionList.Count);
        }
        if (positionList.Contains(firstPosition + new int2(-a, -b)))
        {
            Debug.Log("Duplicate found on index: " + positionList.Count);
        }
        if (positionList.Contains(firstPosition + new int2(-a, b)))
        {
            Debug.Log("Duplicate found on index: " + positionList.Count);
        }
        if (positionList.Contains(firstPosition + new int2(a, -b)))
        {
            Debug.Log("Duplicate found on index: " + positionList.Count);
        }

        positionList.Add(firstPosition + new int2(a, b));
        positionList.Add(firstPosition + new int2(-a, -b));
        positionList.Add(firstPosition + new int2(-a, b));
        positionList.Add(firstPosition + new int2(a, -b));
    }

    private void ValidateGridPosition(ref int x, ref int y)
    {
        x = math.clamp(x, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetWidth() - 1);
        y = math.clamp(y, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetHeight() - 1);
    }

    private static bool IsPositionInsideGrid(int2 gridPosition)
    {
        return
            gridPosition.x >= 0 &&
            gridPosition.y >= 0 &&
            gridPosition.x < PathfindingGridSetup.Instance.pathfindingGrid.GetWidth() &&
            gridPosition.y < PathfindingGridSetup.Instance.pathfindingGrid.GetHeight();
    }

    private static bool IsPositionWalkable(int2 gridPosition)
    {
        return PathfindingGridSetup.Instance.pathfindingGrid.GetGridObject(gridPosition.x, gridPosition.y).IsWalkable();
    }
}
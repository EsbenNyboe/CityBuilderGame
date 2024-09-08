using System.Collections.Generic;
using CodeMonkey.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct UnitSelection : IComponentData
{
}

public partial class UnitControlSystem : SystemBase
{
    private float3 startPosition;

    protected override void OnUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Mouse pressed
            startPosition = UtilsClass.GetMouseWorldPosition();
            SelectionAreaManager.Instance.SelectionArea.gameObject.SetActive(true);
            SelectionAreaManager.Instance.SelectionArea.position = startPosition;
        }

        if (Input.GetMouseButton(0))
        {
            // Mouse held down
            var selectionAreaSize = (float3)UtilsClass.GetMouseWorldPosition() - startPosition;
            SelectionAreaManager.Instance.SelectionArea.localScale = selectionAreaSize;
        }

        if (Input.GetMouseButtonUp(0))
        {
            // Mouse released
            float3 endPosition = UtilsClass.GetMouseWorldPosition();
            SelectionAreaManager.Instance.SelectionArea.gameObject.SetActive(false);

            var lowerLeftPosition = new float3(math.min(startPosition.x, endPosition.x),
                math.min(startPosition.y, endPosition.y), 0);
            var upperRightPosition = new float3(math.max(startPosition.x, endPosition.x),
                math.max(startPosition.y, endPosition.y), 0);

            bool selectOnlyOneEntity = false;
            var selectionAreaSize = math.distance(lowerLeftPosition, upperRightPosition);
            var selectionAreaMinSize = 2f;
            if (selectionAreaSize < selectionAreaMinSize)
            {
                lowerLeftPosition += new float3(-1, -1, 0) * (selectionAreaMinSize - selectionAreaSize) * 0.5f;
                upperRightPosition += new float3(1, 1, 0) * (selectionAreaMinSize - selectionAreaSize) * 0.5f;
                selectOnlyOneEntity = true;
            }


            var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<UnitSelection>>().WithEntityAccess())
            {
                entityCommandBuffer.RemoveComponent(entity, typeof(UnitSelection));
            }

            int selectedEntityCount = 0;
            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (selectOnlyOneEntity && selectedEntityCount > 0)
                {
                    continue;
                }

                var entityPosition = localTransform.ValueRO.Position;
                if (entityPosition.x >= lowerLeftPosition.x &&
                    entityPosition.y >= lowerLeftPosition.y &&
                    entityPosition.x <= upperRightPosition.x &&
                    entityPosition.y <= upperRightPosition.y)
                {
                    entityCommandBuffer.AddComponent(entity, new UnitSelection());
                    selectedEntityCount++;
                }
            }

            entityCommandBuffer.Playback(EntityManager);
        }

        if (Input.GetMouseButtonDown(1) && !Input.GetKey(KeyCode.LeftControl))
        {
            // Right mouse button down
            OrderPathFindingForSelectedUnits();
        }
    }

    private void OrderPathFindingForSelectedUnits()
    {
        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

        var mousePosition = UtilsClass.GetMouseWorldPosition();
        var cellSize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();

        var gridCenterModifier = new Vector3(1, 1) * cellSize * 0.5f;
        var targetGridPosition = mousePosition + gridCenterModifier;

        PathfindingGridSetup.Instance.pathfindingGrid.GetXY(targetGridPosition, out var targetX, out var targetY);
        ValidateGridPosition(ref targetX, ref targetY);
        var targetGridCell = new int2(targetX, targetY);

        List<int2> movePositionList = GetCellListAroundTargetCell(targetGridCell, 20);

        for (int i = 0; i < movePositionList.Count; i++)
        {
            for (int j = i + 1; j < movePositionList.Count; j++)
            {
                if (movePositionList[i].x == movePositionList[j].x && movePositionList[i].y == movePositionList[j].y)
                {
                    Debug.Log("Position list contains duplicate: " + movePositionList[i]);
                }
            }
        }

        if (IsPositionInsideGrid(targetGridCell))
        {
            if (IsPositionWalkable(targetGridCell))
            {
                MoveUnitsToWalkableArea(movePositionList, entityCommandBuffer);
            }
            else
            {
                MoveUnitsToHarvestableCell(movePositionList, entityCommandBuffer);
            }
        }

        entityCommandBuffer.Playback(EntityManager);
    }

    private void MoveUnitsToHarvestableCell(List<int2> movePositionList, EntityCommandBuffer entityCommandBuffer)
    {
        var positionIndex = 0;
        var harvestPosition = movePositionList[positionIndex];

        foreach (var (unitSelection, localTransform, entity) in SystemAPI
                     .Query<RefRO<UnitSelection>, RefRO<LocalTransform>>().WithPresent<HarvestingUnit>()
                     .WithEntityAccess())
        {
            PathfindingGridSetup.Instance.pathfindingGrid.GetXY(localTransform.ValueRO.Position, out var startX,
                out var startY);
            ValidateGridPosition(ref startX, ref startY);

            // TODO: Insert better pathfinding to nearby walkable cell, between end and start, and use that cell as endPosition

            if (harvestPosition.x > startX)
            {
                harvestPosition.x--;
            }
            else if (harvestPosition.x < startX)
            {
                harvestPosition.x++;
            }

            if (harvestPosition.y > startY)
            {
                harvestPosition.y--;
            }
            else if (harvestPosition.y < startY)
            {
                harvestPosition.y++;
            }

            bool walkableCellFound = IsPositionInsideGrid(harvestPosition) && IsPositionWalkable(harvestPosition);
            if (!walkableCellFound)
            {
                walkableCellFound = TryGetWalkablePosition(movePositionList, localTransform, out harvestPosition,
                    ref positionIndex,
                    out startX, out startY);

                if (!walkableCellFound)
                {
                    continue;
                }
            }
            

            entityCommandBuffer.AddComponent(entity, new PathfindingParams
            {
                StartPosition = new int2(startX, startY),
                EndPosition = harvestPosition
            });

            // TODO: Refactor
            var harvestingUnit = EntityManager.GetComponentData<HarvestingUnit>(entity);
            var targetIsOutOfReach = harvestingUnit.TargetIsOutOfReach;
            var target = harvestingUnit.Target;
            var hasTarget = target.x != -1 && target.y != -1;
            if (hasTarget)
            {
                SetDegradationState(target.x, target.y, false);

                foreach (var otherHarvestingUnit in SystemAPI.Query<RefRW<HarvestingUnit>>().WithAll<HarvestingUnit>())
                {
                    // notify all harvestingUnits that a tree has been abandoned
                    otherHarvestingUnit.ValueRW.IsHarvesting = false;
                }
            }

            EntityManager.SetComponentEnabled<HarvestingUnit>(entity, true);
            EntityManager.SetComponentData(entity, new HarvestingUnit
            {
                IsHarvesting = false,
                Target = harvestPosition,
                TargetIsOutOfReach = targetIsOutOfReach
            });
        }
    }

    //private static int2 FindWalkableNeighbourCell(int attempts, int maxAttempts, List<int2> movePositionList,
    //    int2 target)
    //{
    //    attempts++;
    //    if (attempts > maxAttempts)
    //    {
    //        Debug.LogWarning("Nearby neighbours are not walkable");
    //        return target;
    //    }

    //    // browse nearby cells
    //    var neighbour = new int2();

    //    if (!PathfindingGridSetup.Instance.pathfindingGrid.GetGridObject(neighbour.x, neighbour.y).IsWalkable())
    //    {

    //    }

    //    neighbour = TryGetWalkableNeighbour(target, ref neighbour, 1, 0);

    //    return neighbour;
    //}

    //private static bool TryGetWalkableNeighbour(int2 target, ref int2 neighbour, int xAdd, int yAdd)
    //{
    //    if (!PathfindingGridSetup.Instance.pathfindingGrid.GetGridObject(neighbour.x, neighbour.y).IsWalkable())
    //    {
    //        neighbour.x = target.x + xAdd;
    //        neighbour.y = target.y + yAdd;

    //        neighbour = TryGetWalkableNeighbour(target, neighbour, xAdd, yAdd);
    //    }

    //    return neighbour;
    //}

    private void MoveUnitsToWalkableArea(List<int2> movePositionList, EntityCommandBuffer entityCommandBuffer)
    {
        int positionIndex = 0;
        foreach (var (unitSelection, localTransform, entity) in SystemAPI
                     .Query<RefRO<UnitSelection>, RefRO<LocalTransform>>().WithPresent<HarvestingUnit>()
                     .WithEntityAccess())
        {
            if (!TryGetWalkablePosition(movePositionList, localTransform, out var endPosition, ref positionIndex,
                    out var startX, out var startY))
            {
                continue;
            }

            entityCommandBuffer.AddComponent(entity, new PathfindingParams
            {
                StartPosition = new int2(startX, startY),
                EndPosition = endPosition
            });

            // TODO: Refactor

            var harvestingUnit = EntityManager.GetComponentData<HarvestingUnit>(entity);
            var target = harvestingUnit.Target;

            var hasTarget = target.x != -1 && target.y != -1;
            if (hasTarget)
            {
                SetDegradationState(target.x, target.y, false);

                foreach (var otherHarvestingUnit in SystemAPI.Query<RefRW<HarvestingUnit>>().WithAll<HarvestingUnit>())
                {
                    // notify all harvestingUnits that a tree has been abandoned
                    otherHarvestingUnit.ValueRW.IsHarvesting = false;
                }
            }

            EntityManager.SetComponentEnabled<HarvestingUnit>(entity, false);
            EntityManager.SetComponentData(entity, new HarvestingUnit
            {
                IsHarvesting = false,
                Target = new int2(-1, -1)
            });
        }
    }

    private bool TryGetWalkablePosition(List<int2> movePositionList, RefRO<LocalTransform> localTransform,
        out int2 endPosition,
        ref int positionIndex, out int startX, out int startY)
    {
        endPosition = movePositionList[positionIndex];
        positionIndex = (positionIndex + 1) % movePositionList.Count;
        bool positionIsValid = false;

        int maxAttempts = 100;
        int attempts = 0;
        while (!positionIsValid)
        {
            if (IsPositionInsideGrid(endPosition) && IsPositionWalkable(endPosition))
            {
                positionIsValid = true;
            }
            else
            {
                endPosition = movePositionList[positionIndex];
                positionIndex = (positionIndex + 1) % movePositionList.Count;
            }

            attempts++;
            if (attempts > maxAttempts)
            {
                // Hack:
                positionIsValid = true;
            }
        }

        if (attempts > maxAttempts)
        {
            Debug.Log("Could not find valid position target... canceling move order");
            startX = 0;
            startY = 0;
            return false;
        }

        PathfindingGridSetup.Instance.pathfindingGrid.GetXY(localTransform.ValueRO.Position, out startX, out startY);
        ValidateGridPosition(ref startX, ref startY);
        return true;
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
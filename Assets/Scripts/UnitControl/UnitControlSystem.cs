using System.Collections.Generic;
using CodeMonkey.Utils;
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

            var lowerLeftPosition = new float3(math.min(startPosition.x, endPosition.x), math.min(startPosition.y, endPosition.y), 0);
            var upperRightPosition = new float3(math.max(startPosition.x, endPosition.x), math.max(startPosition.y, endPosition.y), 0);

            var selectOnlyOneEntity = false;
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

            var selectedEntityCount = 0;
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
        var cellSize = GridSetup.Instance.PathGrid.GetCellSize();

        var gridCenterModifier = new Vector3(1, 1) * cellSize * 0.5f;
        var targetGridPosition = mousePosition + gridCenterModifier;

        GridSetup.Instance.PathGrid.GetXY(targetGridPosition, out var targetX, out var targetY);
        ValidateGridPosition(ref targetX, ref targetY);
        var targetGridCell = new int2(targetX, targetY);

        var movePositionList = GetCellListAroundTargetCell(targetGridCell, 20);

        for (var i = 0; i < movePositionList.Count; i++)
        {
            for (var j = i + 1; j < movePositionList.Count; j++)
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
                MoveUnitsToHarvestableCell(targetGridCell, entityCommandBuffer);
            }
        }

        entityCommandBuffer.Playback(EntityManager);
    }

    private void MoveUnitsToHarvestableCell(int2 targetGridCell, EntityCommandBuffer entityCommandBuffer)
    {
        foreach (var (unitSelection, localTransform, entity) in SystemAPI
                     .Query<RefRO<UnitSelection>, RefRO<LocalTransform>>().WithPresent<HarvestingUnit>().WithEntityAccess())
        {
            GridSetup.Instance.PathGrid.GetXY(localTransform.ValueRO.Position, out var startX, out var startY);
            ValidateGridPosition(ref startX, ref startY);

            var endPosition = targetGridCell;

            // TODO: Insert better pathfinding to nearby walkable cell, between end and start, and use that cell as endPosition
            if (endPosition.x > startX)
            {
                endPosition.x--;
            }
            else if (endPosition.x < startX)
            {
                endPosition.x++;
            }

            if (endPosition.y > startY)
            {
                endPosition.y--;
            }
            else if (endPosition.y < startY)
            {
                endPosition.y++;
            }

            entityCommandBuffer.AddComponent(entity, new PathfindingParams
            {
                StartPosition = new int2(startX, startY),
                EndPosition = endPosition
            });

            // TODO: Refactor
            var target = EntityManager.GetComponentData<HarvestingUnit>(entity).Target;
            var hasTarget = target.x != -1 && target.y != -1;
            if (hasTarget)
            {
                SetDegradationState(target.x, target.y, false);

                foreach (var harvestingUnit in SystemAPI.Query<RefRW<HarvestingUnit>>().WithAll<HarvestingUnit>())
                {
                    // notify all harvestingUnits that a tree has been abandoned
                    // how, though?
                }
            }

            EntityManager.SetComponentEnabled<HarvestingUnit>(entity, true);
            EntityManager.SetComponentData(entity, new HarvestingUnit
            {
                Target = targetGridCell
            });
        }
    }

    private void MoveUnitsToWalkableArea(List<int2> movePositionList, EntityCommandBuffer entityCommandBuffer)
    {
        var positionIndex = 0;
        foreach (var (unitSelection, localTransform, entity) in SystemAPI.Query<RefRO<UnitSelection>, RefRO<LocalTransform>>()
                     .WithPresent<HarvestingUnit>().WithEntityAccess())
        {
            var endPosition = movePositionList[positionIndex];
            positionIndex = (positionIndex + 1) % movePositionList.Count;
            var positionIsValid = false;

            var maxAttempts = 100;
            var attempts = 0;
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
                continue;
            }

            GridSetup.Instance.PathGrid.GetXY(localTransform.ValueRO.Position, out var startX, out var startY);
            ValidateGridPosition(ref startX, ref startY);

            entityCommandBuffer.AddComponent(entity, new PathfindingParams
            {
                StartPosition = new int2(startX, startY),
                EndPosition = endPosition
            });

            // TODO: Refactor
            var target = EntityManager.GetComponentData<HarvestingUnit>(entity).Target;
            var hasTarget = target.x != -1 && target.y != -1;
            if (hasTarget)
            {
                SetDegradationState(target.x, target.y, false);

                foreach (var harvestingUnit in SystemAPI.Query<RefRW<HarvestingUnit>>().WithAll<HarvestingUnit>())
                {
                    // TODO: This is duplicated elsewhere, why?
                    // notify all harvestingUnits that a tree has been abandoned
                    // how, though?
                }
            }

            EntityManager.SetComponentEnabled<HarvestingUnit>(entity, false);
            EntityManager.SetComponentData(entity, new HarvestingUnit
            {
                Target = new int2(-1, -1)
            });
        }
    }

    private void SetDegradationState(int targetX, int targetY, bool state)
    {
        Debug.LogWarning("Refactor this?");
        return;
        foreach (var (localTransform, unitDegradation) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<UnitDegradation>>())
        {
            GridSetup.Instance.PathGrid.GetXY(localTransform.ValueRO.Position, out var x, out var y);
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

        for (var i = 1; i < ringCount; i++)
        {
            for (var j = 1; j < i; j++)
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
            for (var i = 0; i < positionList.Count; i++)
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
        x = math.clamp(x, 0, GridSetup.Instance.PathGrid.GetWidth() - 1);
        y = math.clamp(y, 0, GridSetup.Instance.PathGrid.GetHeight() - 1);
    }

    private static bool IsPositionInsideGrid(int2 gridPosition)
    {
        return
            gridPosition.x >= 0 &&
            gridPosition.y >= 0 &&
            gridPosition.x < GridSetup.Instance.PathGrid.GetWidth() &&
            gridPosition.y < GridSetup.Instance.PathGrid.GetHeight();
    }

    private static bool IsPositionWalkable(int2 gridPosition)
    {
        return GridSetup.Instance.PathGrid.GetGridObject(gridPosition.x, gridPosition.y).IsWalkable();
    }
}
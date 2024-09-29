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
        PathingHelpers.ValidateGridPosition(ref targetX, ref targetY);
        var targetGridCell = new int2(targetX, targetY);

        var movePositionList = PathingHelpers.GetCellListAroundTargetCell(targetGridCell, 20);

        // DEBUGGING:
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

        if (PathingHelpers.IsPositionInsideGrid(targetGridCell))
        {
            if (PathingHelpers.IsPositionWalkable(targetGridCell))
            {
                MoveUnitsToWalkableArea(movePositionList, entityCommandBuffer);
            }
            else
            {
                MoveUnitsToHarvestableCell(movePositionList, entityCommandBuffer, targetGridCell);
            }
        }

        entityCommandBuffer.Playback(EntityManager);
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
                if (PathingHelpers.IsPositionInsideGrid(endPosition) && PathingHelpers.IsPositionWalkable(endPosition))
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
            PathingHelpers.ValidateGridPosition(ref startX, ref startY);

            entityCommandBuffer.AddComponent(entity, new PathfindingParams
            {
                StartPosition = new int2(startX, startY),
                EndPosition = endPosition
            });

            EntityManager.SetComponentEnabled<HarvestingUnit>(entity, false);
            EntityManager.SetComponentData(entity, new HarvestingUnit
            {
                Target = new int2(-1, -1)
            });

            AbandonCellIfOccupying(startX, startY, entity);
        }
    }

    private void MoveUnitsToHarvestableCell(List<int2> movePositionList, EntityCommandBuffer entityCommandBuffer, int2 targetGridCell)
    {
        var walkableNeighbourCells = new List<int2>();

        if (!TryGetWalkableNeighbourCells(targetGridCell, walkableNeighbourCells))
        {
            Debug.Log("No walkable neighbour cell found. Please try again!");
            return;
        }

        var positionIndex = 0;
        foreach (var (unitSelection, localTransform, entity) in SystemAPI
                     .Query<RefRO<UnitSelection>, RefRO<LocalTransform>>().WithPresent<HarvestingUnit>().WithEntityAccess())
        {
            var endPosition = walkableNeighbourCells[positionIndex];
            positionIndex = (positionIndex + 1) % walkableNeighbourCells.Count;

            GridSetup.Instance.PathGrid.GetXY(localTransform.ValueRO.Position, out var startX, out var startY);
            PathingHelpers.ValidateGridPosition(ref startX, ref startY);
            entityCommandBuffer.AddComponent(entity, new PathfindingParams
            {
                StartPosition = new int2(startX, startY),
                EndPosition = endPosition
            });

            EntityManager.SetComponentEnabled<HarvestingUnit>(entity, true);
            EntityManager.SetComponentData(entity, new HarvestingUnit
            {
                Target = targetGridCell
            });

            AbandonCellIfOccupying(startX, startY, entity);
        }
    }

    private static bool TryGetWalkableNeighbourCells(int2 targetGridCell, List<int2> walkableNeighbourCells)
    {
        const int maxPossibleNeighbours = 8;
        for (var i = 0; i < maxPossibleNeighbours; i++)
        {
            PathingHelpers.GetNeighbourCell(i, targetGridCell.x, targetGridCell.y, out var neighbourX, out var neighbourY);

            if (PathingHelpers.IsPositionInsideGrid(neighbourX, neighbourY) &&
                GridSetup.Instance.PathGrid.GetGridObject(neighbourX, neighbourY).IsWalkable())
            {
                walkableNeighbourCells.Add(new int2(neighbourX, neighbourY));
            }
        }

        return walkableNeighbourCells.Count > 0;
    }

    private static void AbandonCellIfOccupying(int startX, int startY, Entity entity)
    {
        var occupationCell = GridSetup.Instance.OccupationGrid.GetGridObject(startX, startY);
        if (occupationCell.EntityIsOwner(entity))
        {
            occupationCell.SetOccupied(Entity.Null);
        }
    }
}
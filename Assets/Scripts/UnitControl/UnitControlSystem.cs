using System.Collections.Generic;
using System.Text;
using CodeMonkey.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct UnitSelection : IComponentData
{
}

[UpdateAfter(typeof(GridManagerSystem))]
public partial class UnitControlSystem : SystemBase
{
    private float3 startPosition;
    private SystemHandle _gridManagerSystemHandle;

    protected override void OnCreate()
    {
        _gridManagerSystemHandle = World.GetExistingSystem<GridManagerSystem>();
    }

    protected override void OnUpdate()
    {
        var isHoldingSpawnItem = SpawnMenuManager.Instance.HasSelection();

        if (isHoldingSpawnItem)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.A) && Input.GetKey(KeyCode.LeftControl))
        {
            SelectAllUnits();
        }

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
            var selectionAreaMinSize = SelectionAreaManager.Instance.GetMinSelectionArea();
            if (selectionAreaSize < selectionAreaMinSize)
            {
                lowerLeftPosition += new float3(-1, -1, 0) * (selectionAreaMinSize - selectionAreaSize) * 0.5f;
                upperRightPosition += new float3(1, 1, 0) * (selectionAreaMinSize - selectionAreaSize) * 0.5f;
                selectOnlyOneEntity = true;
            }

            var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

            TryRemoveAllUnitSelections(entityCommandBuffer);

            var selectedEntityCount = 0;
            foreach (var (_, localTransform, entity) in SystemAPI.Query<RefRO<Selectable>, RefRO<LocalTransform>>().WithEntityAccess())
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
                    var currentName = EntityManager.GetName(entity);
                    if (!currentName.Contains("SelectedUnit"))
                    {
                        EntityManager.SetName(entity, new StringBuilder().Append(currentName).Append("SelectedUnit").ToString());
                    }

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

    private void TryRemoveAllUnitSelections(EntityCommandBuffer entityCommandBuffer)
    {
        foreach (var (_, entity) in SystemAPI.Query<RefRO<UnitSelection>>().WithEntityAccess())
        {
            var currentName = EntityManager.GetName(entity);
            if (currentName.Contains("SelectedUnit"))
            {
                currentName = currentName.Replace("SelectedUnit", "");
                EntityManager.SetName(entity, currentName);
            }

            entityCommandBuffer.RemoveComponent(entity, typeof(UnitSelection));
        }
    }

    private void OrderPathFindingForSelectedUnits()
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);

        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

        var mousePosition = UtilsClass.GetMouseWorldPosition();
        var cellSize = 1f; // gridManager currently only supports a cellSize of 1

        var gridCenterModifier = new Vector3(1, 1) * cellSize * 0.5f;
        var targetGridPosition = mousePosition + gridCenterModifier;
        GridHelpers.GetXY(targetGridPosition, out var targetX, out var targetY);

        gridManager.ValidateGridPosition(ref targetX, ref targetY);
        var targetGridCell = new int2(targetX, targetY);

        var movePositionList = gridManager.GetCachedCellListAroundTargetCell(targetGridCell.x, targetGridCell.y);

        // DEBUGGING:
        // for (var i = 0; i < movePositionList.Length; i++)
        // {
        //     for (var j = i + 1; j < movePositionList.Length; j++)
        //     {
        //         if (movePositionList[i].x == movePositionList[j].x && movePositionList[i].y == movePositionList[j].y)
        //         {
        //             Debug.Log("Position list contains duplicate: " + movePositionList[i]);
        //         }
        //     }
        // }

        if (gridManager.IsPositionInsideGrid(targetGridCell))
        {
            if (gridManager.IsWalkable(targetGridCell))
            {
                MoveUnitsToWalkableArea(gridManager, movePositionList, entityCommandBuffer);
            }
            else if (gridManager.IsDamageable(targetGridCell.x, targetGridCell.y))
            {
                MoveUnitsToHarvestableCell(gridManager, entityCommandBuffer, targetGridCell);
            }
        }

        entityCommandBuffer.Playback(EntityManager);
    }

    private void MoveUnitsToWalkableArea(GridManager gridManager, NativeArray<int2> movePositionList, EntityCommandBuffer entityCommandBuffer)
    {
        var positionIndex = 0;
        foreach (var (unitSelection, localTransform, entity) in SystemAPI.Query<RefRO<UnitSelection>, RefRO<LocalTransform>>()
                     .WithPresent<HarvestingUnit>().WithEntityAccess())
        {
            var endPosition = movePositionList[positionIndex];
            positionIndex = (positionIndex + 1) % movePositionList.Length;
            var positionIsValid = false;

            var maxAttempts = 100;
            var attempts = 0;
            while (!positionIsValid)
            {
                if (gridManager.IsPositionInsideGrid(endPosition) && gridManager.IsWalkable(endPosition))
                {
                    positionIsValid = true;
                }
                else
                {
                    endPosition = movePositionList[positionIndex];
                    positionIndex = (positionIndex + 1) % movePositionList.Length;
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

            entityCommandBuffer.RemoveComponent<ChopAnimationTag>(entity);
            SystemAPI.SetComponent(entity, new SpriteTransform
            {
                Position = float3.zero,
                Rotation = quaternion.identity
            });
            entityCommandBuffer.RemoveComponent<HarvestingUnitTag>(entity);
            entityCommandBuffer.RemoveComponent<DeliveringUnitTag>(entity);

            entityCommandBuffer.AddComponent(entity, new TryDeoccupy
            {
                NewTarget = endPosition
            });
        }
    }

    private void MoveUnitsToHarvestableCell(GridManager gridManager, EntityCommandBuffer entityCommandBuffer, int2 targetGridCell)
    {
        var walkableNeighbourCells = new List<int2>();

        if (!TryGetWalkableNeighbourCells(ref gridManager, targetGridCell, walkableNeighbourCells))
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

            entityCommandBuffer.AddComponent<HarvestingUnitTag>(entity);
            SystemAPI.SetComponent(entity, new HarvestingUnit
            {
                Target = targetGridCell
            });
            entityCommandBuffer.RemoveComponent<DeliveringUnitTag>(entity);

            entityCommandBuffer.AddComponent(entity, new TryDeoccupy
            {
                NewTarget = endPosition
            });
        }

        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
    }

    private static bool TryGetWalkableNeighbourCells(ref GridManager gridManager, int2 targetGridCell, List<int2> walkableNeighbourCells)
    {
        gridManager.RandomizeNeighbourSequenceIndex();
        const int maxPossibleNeighbours = 8;
        for (var i = 0; i < maxPossibleNeighbours; i++)
        {
            gridManager.GetSequencedNeighbourCell(targetGridCell.x, targetGridCell.y, out var neighbourX, out var neighbourY);

            if (gridManager.IsPositionInsideGrid(neighbourX, neighbourY) &&
                gridManager.WalkableGrid[gridManager.GetIndex(neighbourX, neighbourY)].IsWalkable)
            {
                walkableNeighbourCells.Add(new int2(neighbourX, neighbourY));
            }
        }

        return walkableNeighbourCells.Count > 0;
    }

    private bool TryAbandonCell(ref GridManager gridManager, int startX, int startY, Entity entity)
    {
        if (gridManager.TryClearOccupant(startX, startY, entity))
        {
            return true;
        }

        return false;
    }

    private void SelectAllUnits()
    {
        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

        foreach (var (_, entity) in SystemAPI.Query<RefRO<Selectable>>().WithEntityAccess())
        {
            entityCommandBuffer.AddComponent(entity, new UnitSelection());
        }

        entityCommandBuffer.Playback(EntityManager);
    }
}
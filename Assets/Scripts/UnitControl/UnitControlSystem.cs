using System.Collections.Generic;
using System.Text;
using CodeMonkey.Utils;
using UnitBehaviours.AutonomousHarvesting;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct UnitSelection : IComponentData
{
}

[UpdateBefore(typeof(PathfindingSystem))]
[UpdateAfter(typeof(PathFollowSystem))]
[UpdateInGroup(typeof(UnitStateSystemGroup))]
public partial class UnitControlSystem : SystemBase
{
    private float3 _mouseStartPosition;
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
            _mouseStartPosition = UtilsClass.GetMouseWorldPosition();
            SelectionAreaManager.Instance.SelectionArea.gameObject.SetActive(true);
            SelectionAreaManager.Instance.SelectionArea.position = _mouseStartPosition;
        }

        if (Input.GetMouseButton(0))
        {
            // Mouse held down
            var selectionAreaSize = (float3)UtilsClass.GetMouseWorldPosition() - _mouseStartPosition;
            SelectionAreaManager.Instance.SelectionArea.localScale = selectionAreaSize;
        }

        if (Input.GetMouseButtonUp(0))
        {
            // Mouse released
            float3 mouseEndPosition = UtilsClass.GetMouseWorldPosition();
            SelectionAreaManager.Instance.SelectionArea.gameObject.SetActive(false);

            var lowerLeftPosition = new float3(math.min(_mouseStartPosition.x, mouseEndPosition.x),
                math.min(_mouseStartPosition.y, mouseEndPosition.y), 0);
            var upperRightPosition = new float3(math.max(_mouseStartPosition.x, mouseEndPosition.x),
                math.max(_mouseStartPosition.y, mouseEndPosition.y), 0);

            var selectOnlyOneEntity = false;
            var selectionAreaSize = math.distance(lowerLeftPosition, upperRightPosition);
            var selectionAreaMinSize = SelectionAreaManager.Instance.GetMinSelectionArea();
            if (selectionAreaSize < selectionAreaMinSize)
            {
                lowerLeftPosition += new float3(-1, -1, 0) * (selectionAreaMinSize - selectionAreaSize) * 0.5f;
                upperRightPosition += new float3(1, 1, 0) * (selectionAreaMinSize - selectionAreaSize) * 0.5f;
                selectOnlyOneEntity = true;
            }

            var ecb = new EntityCommandBuffer(WorldUpdateAllocator);

            DeselectAllUnits(ecb);

            var selectedEntityCount = 0;
            foreach (var (_, localTransform, entity) in SystemAPI.Query<RefRO<Selectable>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
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
                        EntityManager.SetName(entity,
                            new StringBuilder().Append(currentName).Append("SelectedUnit").ToString());
                    }

                    ecb.AddComponent(entity, new UnitSelection());
                    selectedEntityCount++;
                }
            }

            ecb.Playback(EntityManager);
        }

        if (Input.GetMouseButtonDown(1) && !Input.GetKey(KeyCode.LeftControl))
        {
            // Right mouse button down
            OrderPathFindingForSelectedUnits();
        }
    }

    private void SelectAllUnits()
    {
        var ecb = new EntityCommandBuffer(WorldUpdateAllocator);

        foreach (var (_, entity) in SystemAPI.Query<RefRO<Selectable>>().WithEntityAccess())
        {
            ecb.AddComponent(entity, new UnitSelection());
        }

        ecb.Playback(EntityManager);
    }

    private void DeselectAllUnits(EntityCommandBuffer ecb)
    {
        foreach (var (_, entity) in SystemAPI.Query<RefRO<UnitSelection>>().WithEntityAccess())
        {
            var currentName = EntityManager.GetName(entity);
            if (currentName.Contains("SelectedUnit"))
            {
                currentName = currentName.Replace("SelectedUnit", "");
                EntityManager.SetName(entity, currentName);
            }

            ecb.RemoveComponent(entity, typeof(UnitSelection));
        }
    }

    private void OrderPathFindingForSelectedUnits()
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);

        var ecb = new EntityCommandBuffer(WorldUpdateAllocator);

        var mousePosition = UtilsClass.GetMouseWorldPosition();
        var cellSize = 1f; // gridManager currently only supports a cellSize of 1

        var gridCenterModifier = new Vector3(1, 1) * cellSize * 0.5f;
        var targetGridPosition = mousePosition + gridCenterModifier;
        GridHelpers.GetXY(targetGridPosition, out var targetX, out var targetY);

        gridManager.ValidateGridPosition(ref targetX, ref targetY);
        var targetGridCell = new int2(targetX, targetY);

        var movePositionList = gridManager.GetCachedCellListAroundTargetCell(targetGridCell.x, targetGridCell.y);

        if (gridManager.IsPositionInsideGrid(targetGridCell))
        {
            if (gridManager.IsWalkable(targetGridCell))
            {
                MoveUnitsToWalkableArea(ref gridManager, movePositionList, ecb);
            }
            else if (gridManager.IsDamageable(targetGridCell.x, targetGridCell.y))
            {
                MoveUnitsToHarvestableCell(ref gridManager, ecb, targetGridCell);
            }
        }

        ecb.Playback(EntityManager);
        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
    }

    private void MoveUnitsToWalkableArea(ref GridManager gridManager, NativeArray<int2> movePositionList,
        EntityCommandBuffer ecb)
    {
        var positionIndex = 0;
        foreach (var (unitSelection, localTransform, entity) in SystemAPI
                     .Query<RefRO<UnitSelection>, RefRO<LocalTransform>>().WithEntityAccess())
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

            ForceUnitToSelectPath(ecb, ref gridManager, entity, GridHelpers.GetXY(localTransform.ValueRO.Position),
                endPosition);
            ecb.AddComponent<IsIdle>(entity);
        }
    }

    private void MoveUnitsToHarvestableCell(ref GridManager gridManager, EntityCommandBuffer ecb, int2 targetGridCell)
    {
        var walkableNeighbourCells = new List<int2>();

        if (!TryGetWalkableNeighbourCells(ref gridManager, targetGridCell, walkableNeighbourCells))
        {
            Debug.Log("No walkable neighbour cell found. Please try again!");
            return;
        }

        var positionIndex = 0;
        foreach (var (unitSelection, localTransform, entity) in SystemAPI
                     .Query<RefRO<UnitSelection>, RefRO<LocalTransform>>()
                     .WithEntityAccess())
        {
            var endPosition = walkableNeighbourCells[positionIndex];
            positionIndex = (positionIndex + 1) % walkableNeighbourCells.Count;

            ForceUnitToSelectPath(ecb, ref gridManager, entity, GridHelpers.GetXY(localTransform.ValueRO.Position),
                endPosition);
            ecb.AddComponent<IsSeekingTree>(entity);
        }
    }

    private static bool TryGetWalkableNeighbourCells(ref GridManager gridManager, int2 targetGridCell,
        List<int2> walkableNeighbourCells)
    {
        const int maxPossibleNeighbours = 8;
        for (var i = 0; i < maxPossibleNeighbours; i++)
        {
            var neighbourCell = gridManager.GetNeighbourCell(i, targetGridCell);

            if (gridManager.IsPositionInsideGrid(neighbourCell) &&
                gridManager.WalkableGrid[gridManager.GetIndex(neighbourCell)].IsWalkable)
            {
                walkableNeighbourCells.Add(new int2(neighbourCell));
            }
        }

        return walkableNeighbourCells.Count > 0;
    }

    private void ForceUnitToSelectPath(EntityCommandBuffer ecb, ref GridManager gridManager, Entity entity,
        int2 startPosition, int2 endPosition)
    {
        SystemAPI.SetComponent(entity, new SpriteTransform
        {
            Position = float3.zero,
            Rotation = quaternion.identity
        });

        if (PathHelpers.TrySetPath(ecb, entity, startPosition, endPosition))
        {
            TryResetGridCell(ref gridManager, startPosition, entity);
        }

        RemoveAllBehaviours(ecb, entity);
    }

    private static void RemoveAllBehaviours(EntityCommandBuffer ecb, Entity entity)
    {
        ecb.RemoveComponent<ChopAnimationTag>(entity);
        ecb.RemoveComponent<IsHarvesting>(entity);
        ecb.RemoveComponent<IsSeekingTree>(entity);
        ecb.RemoveComponent<IsSeekingDropPoint>(entity);

        ecb.RemoveComponent<IsSeekingBed>(entity);
        ecb.RemoveComponent<IsSleeping>(entity);

        ecb.RemoveComponent<IsIdle>(entity);
        ecb.RemoveComponent<IsTickListener>(entity);
    }


    private void TryResetGridCell(ref GridManager gridManager, int2 position, Entity entity)
    {
        var isOccupant = gridManager.TryClearOccupant(position, entity);
        if (isOccupant && gridManager.IsBed(position) && !gridManager.IsWalkable(position))
        {
            gridManager.SetIsWalkable(position, true);
        }
    }
}
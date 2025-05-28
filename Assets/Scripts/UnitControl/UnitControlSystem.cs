using System.Collections.Generic;
using System.Text;
using CodeMonkey.Utils;
using Debugging;
using Grid;
using SpriteTransformNS;
using SystemGroups;
using UnitBehaviours.AutonomousHarvesting;
using UnitBehaviours.Idle;
using UnitBehaviours.Pathing;
using UnitBehaviours.Sleeping;
using UnitBehaviours.Tags;
using UnitBehaviours.Talking;
using UnitBehaviours.Targeting;
using UnitSpawn.Spawning;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitControl
{
    [UpdateBefore(typeof(PathfindingSystem))]
    [UpdateAfter(typeof(PathFollowSystem))]
    [UpdateInGroup(typeof(UnitStateSystemGroup))]
    public partial class UnitControlSystem : SystemBase
    {
        private float3 _mouseStartPosition;

        protected override void OnUpdate()
        {
            if (GetEntityQuery(typeof(UnitSelection)).CalculateEntityCount() < 1)
            {
                return;
            }

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

                using var ecb = new EntityCommandBuffer(Allocator.Temp);

                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                {
                    DeselectAllUnits();
                }

                var selectedEntityCount = 0;
                foreach (var (_, localTransform, entity) in SystemAPI.Query<RefRO<Selectable>, RefRO<LocalTransform>>()
                             .WithEntityAccess().WithNone<UnitSelection>())
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
            using var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<Selectable>>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new UnitSelection());
            }

            ecb.Playback(EntityManager);
        }

        private void DeselectAllUnits()
        {
            using var ecb = new EntityCommandBuffer(Allocator.Temp);

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

            ecb.Playback(EntityManager);
        }

        private void OrderPathFindingForSelectedUnits()
        {
            var gridManager = SystemAPI.GetSingleton<GridManager>();

            using var ecb = new EntityCommandBuffer(Allocator.Temp);

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

            SystemAPI.SetSingleton(gridManager);
            ecb.Playback(EntityManager);
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
                    DebugHelper.Log("Could not find valid position target... canceling move order");
                    continue;
                }

                ForceUnitToSelectPath(ecb, ref gridManager, entity, GridHelpers.GetXY(localTransform.ValueRO.Position),
                    endPosition);
            }
        }

        private void MoveUnitsToHarvestableCell(ref GridManager gridManager, EntityCommandBuffer ecb, int2 targetGridCell)
        {
            var walkableNeighbourCells = new List<int2>();

            if (!TryGetWalkableNeighbourCells(ref gridManager, targetGridCell, walkableNeighbourCells))
            {
                DebugHelper.Log("No walkable neighbour cell found. Please try again!");
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
            if (PathHelpers.TrySetPath(ecb, gridManager, entity, startPosition, endPosition))
            {
                TryResetGridCell(ref gridManager, startPosition, entity);
            }

            RemoveAllBehaviours(ecb, entity);
            ResetAllBehaviourState(entity);
            ecb.AddComponent<IsIdle>(entity);
        }

        private void ResetAllBehaviourState(Entity entity)
        {
            SystemAPI.SetComponent(entity, new SpriteTransform
            {
                Position = float3.zero,
                Rotation = quaternion.identity
            });

            SystemAPI.SetComponent(entity, new TargetFollow
            {
                Target = Entity.Null,
                DesiredRange = -1,
                CurrentDistanceToTarget = math.INFINITY
            });

            SystemAPI.SetComponent(entity, new PathFollow
            {
                PathIndex = -1,
                MoveSpeedMultiplier = 1
            });
        }

        private static void RemoveAllBehaviours(EntityCommandBuffer ecb, Entity entity)
        {
            ecb.RemoveComponent<AttackAnimation>(entity);
            ecb.RemoveComponent<IsHarvesting>(entity);
            ecb.RemoveComponent<IsSeekingTree>(entity);
            ecb.RemoveComponent<IsSeekingDropPoint>(entity);

            ecb.RemoveComponent<IsSleeping>(entity);
            ecb.RemoveComponent<IsSeekingBed>(entity);

            ecb.RemoveComponent<IsIdle>(entity);
            ecb.RemoveComponent<IsTickListener>(entity);

            ecb.RemoveComponent<IsAttemptingMurder>(entity);
            ecb.RemoveComponent<IsMurdering>(entity);

            ecb.RemoveComponent<IsTalkative>(entity);
            ecb.RemoveComponent<IsTalking>(entity);
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
}
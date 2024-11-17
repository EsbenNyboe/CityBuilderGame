using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Grid
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct GridManagerHighLevelSortingSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;

        // Debugging
        private int _debugListCurrent;
        private int _debugListSuccessHitsCurrent;
        private NativeList<int2> _debugList;
        private NativeList<int2> _debugListSuccessHits;
        private NativeHashMap<int2, int> _debugSectionMap;
        private bool _hasQuickDebuggedWholeSection;
        private int _currentSectionBeingDebugged;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DebugToggleManager>();
            _gridManagerSystemHandle = state.World.GetExistingSystem(typeof(GridManagerSystem));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SortingProcess(ref state);
        }

        private void SortingProcess(ref SystemState state)
        {
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var isDebug = SystemAPI.GetSingleton<DebugToggleManager>().IsDebugging;

            if (!TryGetWalkableCells(gridManager, out var walkableNodeQueue))
            {
                return;
            }

            var walkablesCount = walkableNodeQueue.Count;
            var openNodes = ConvertToHashSet(walkableNodeQueue);

            if (isDebug)
            {
                AllocateDebugContainers(walkablesCount);
            }

            var closedNodes = new NativeParallelHashMap<int2, WalkableSectionNode>(walkablesCount, Allocator.Temp);
            SortWalkableSections(ref gridManager, openNodes, closedNodes, isDebug);

            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);

            if (isDebug)
            {
                DebugDrawSections(gridManager);
                DebugDrawSearchAlgorithm(ref state);
                DisposeDebugContainers();
            }

            openNodes.Dispose();
            closedNodes.Dispose();
        }

        private static bool TryGetWalkableCells(GridManager gridManager, out NativeQueue<int2> walkableNodeQueue)
        {
            walkableNodeQueue = new NativeQueue<int2>(Allocator.Temp);
            var gridSize = gridManager.Height * gridManager.Width;

            for (var i = 0; i < gridSize; i++)
            {
                var walkableCell = gridManager.WalkableGrid[i];
                if (walkableCell.IsWalkable)
                {
                    walkableNodeQueue.Enqueue(gridManager.GetXY(i));
                }
            }

            if (walkableNodeQueue.Count > 0)
            {
                return true;
            }

            // There's nowhere for anyone to walk
            walkableNodeQueue.Dispose();
            return false;
        }

        private static NativeParallelHashSet<int2> ConvertToHashSet(NativeQueue<int2> walkableNodeQueue)
        {
            var openNodes = new NativeParallelHashSet<int2>(walkableNodeQueue.Count, Allocator.Temp);

            while (walkableNodeQueue.Count > 0)
            {
                openNodes.Add(walkableNodeQueue.Dequeue());
            }

            walkableNodeQueue.Dispose();
            return openNodes;
        }

        private void SortWalkableSections(ref GridManager gridManager, NativeParallelHashSet<int2> openNodes,
            NativeParallelHashMap<int2, WalkableSectionNode> closedNodes, bool isDebug)
        {
            var neighbourDeltas = gridManager.NeighbourDeltas;
            var currentSection = -1;
            while (openNodes.Count() > 0)
            {
                // Add first node to section
                using var enumerator = openNodes.GetEnumerator();
                enumerator.MoveNext();
                var currentCell = enumerator.Current;
                openNodes.Remove(currentCell);
                var currentNode = new WalkableSectionNode
                {
                    GridCell = currentCell,
                    NodeSource = -1
                };
                currentSection++;
                gridManager.SetWalkableSection(currentCell, currentSection);
                closedNodes.Add(currentCell, currentNode);
                if (isDebug)
                {
                    AddSectionStartToDebugData(currentCell, currentSection);
                }

                // Add all nodes to this section, by searching all neighbours
                while (currentNode.NeighboursSearched < neighbourDeltas.Length)
                {
                    // Search node-neighbour
                    var neighbourCell = currentNode.GridCell + neighbourDeltas[currentNode.NeighboursSearched];
                    if (isDebug)
                    {
                        AddSearchStepToDebugData(neighbourCell);
                    }

                    currentNode.NeighboursSearched++;
                    if (openNodes.Remove(neighbourCell))
                    {
                        // Add node to section
                        var newNode = new WalkableSectionNode
                        {
                            GridCell = neighbourCell,
                            NodeSource = currentNode.GridCell
                        };
                        gridManager.SetWalkableSection(neighbourCell, currentSection);
                        closedNodes.TryAdd(currentNode.GridCell, currentNode);

                        // Interrupt current neighbour-search, and continue neighbour-search at new node
                        currentNode = newNode;

                        if (isDebug)
                        {
                            AddSearchHitToDebugData(neighbourCell, currentSection);
                        }
                    }

                    while (currentNode.NeighboursSearched >= neighbourDeltas.Length && currentNode.NodeSource.x > -1)
                    {
                        // Resume interrupted neighbour-search at the node, where we came from
                        var previousNode = closedNodes[currentNode.NodeSource];
                        currentNode = previousNode;
                    }
                }
            }
        }

        private struct WalkableSectionNode
        {
            public int2 GridCell;
            public int2 NodeSource;
            public int NeighboursSearched;
        }

        #region Debugging

        private void AllocateDebugContainers(int debugLength)
        {
            _debugList = new NativeList<int2>(debugLength, Allocator.Temp);
            _debugListSuccessHits = new NativeList<int2>(debugLength, Allocator.Temp);
            _debugSectionMap = new NativeHashMap<int2, int>(debugLength, Allocator.Temp);
        }

        private void DisposeDebugContainers()
        {
            _debugList.Dispose();
            _debugListSuccessHits.Dispose();
            _debugSectionMap.Dispose();
        }

        private void AddSectionStartToDebugData(int2 currentCell, int currentSection)
        {
            _debugList.Add(currentCell);
            _debugListSuccessHits.Add(currentCell);
            _debugSectionMap.Add(currentCell, currentSection);
        }

        private void AddSearchStepToDebugData(int2 neighbourCell)
        {
            _debugList.Add(neighbourCell);
        }

        private void AddSearchHitToDebugData(int2 neighbourCell, int currentSection)
        {
            _debugListSuccessHits.Add(neighbourCell);
            _debugSectionMap.Add(neighbourCell, currentSection);
        }

        private void DebugDrawSections(GridManager gridManager)
        {
            var walkableGrid = gridManager.WalkableGrid;
            for (var i = 0; i < walkableGrid.Length; i++)
            {
                if (gridManager.IsWalkable(i))
                {
                    DebugDrawCell(gridManager.GetXY(i), GetSectionColor(gridManager.WalkableGrid[i].Section));
                }
            }
        }

        private void DebugDrawSearchAlgorithm(ref SystemState state)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                _hasQuickDebuggedWholeSection = false;
            }

            if (Input.GetKeyDown(KeyCode.Return) ||
                (Input.GetKey(KeyCode.Return) && Input.GetKey(KeyCode.LeftShift) && !_hasQuickDebuggedWholeSection))
            {
                if (_debugSectionMap.TryGetValue(_debugList[_debugListCurrent],
                        out var currentSection))
                {
                    if (_currentSectionBeingDebugged != currentSection)
                    {
                        _currentSectionBeingDebugged = currentSection;
                        _hasQuickDebuggedWholeSection = true;
                        return;
                    }
                }

                if (_debugListSuccessHitsCurrent == 0)
                {
                    if (_debugListCurrent == 0)
                    {
                        _debugListSuccessHitsCurrent++;
                        if (_debugListSuccessHitsCurrent >= _debugListSuccessHits.Length)
                        {
                            _debugListSuccessHitsCurrent = 0;
                        }
                    }
                }
                else if (_debugListSuccessHits[_debugListSuccessHitsCurrent].Equals(_debugList[_debugListCurrent]))
                {
                    _debugListSuccessHitsCurrent++;
                    if (_debugListSuccessHitsCurrent >= _debugListSuccessHits.Length)
                    {
                        _debugListSuccessHitsCurrent = 0;
                    }
                }

                _debugListCurrent++;
                if (_debugListCurrent >= _debugList.Length)
                {
                    _debugListCurrent = 0;
                }
            }


            var color = Color.red;
            if (_debugListCurrent == 0)
            {
                color = Color.blue;
            }
            else if (_debugListSuccessHitsCurrent != 0 && _debugListSuccessHits[_debugListSuccessHitsCurrent]
                         .Equals(_debugList[_debugListCurrent]))
            {
                color = Color.green;
            }

            DebugDrawCell(_debugList[_debugListCurrent], color);
        }

        private static void DebugDrawCell(int2 cell, Color color)
        {
            var padding = 0.2f;
            var offset = 1f - padding;
            var debugPosition = new Vector3(cell.x - 0.5f + padding, cell.y - 0.5f + padding, 0);
            Debug.DrawLine(debugPosition, debugPosition + new Vector3(+offset, +0), color);
            Debug.DrawLine(debugPosition, debugPosition + new Vector3(+0, +offset), color);
            Debug.DrawLine(debugPosition + new Vector3(+offset, +0), debugPosition + new Vector3(+offset, +offset),
                color);
            Debug.DrawLine(debugPosition + new Vector3(+0, +offset), debugPosition + new Vector3(+offset, +offset),
                color);
        }

        private Color GetSectionColor(int sectionKey)
        {
            var random = new Random((uint)(sectionKey * 999 + 1));
            var color = new Color
            {
                r = random.NextFloat(),
                g = random.NextFloat(),
                b = random.NextFloat(),
                a = 1
            };
            return color;
        }

        #endregion
    }
}
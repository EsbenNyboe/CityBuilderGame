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

        private int _debugListCurrent;
        private int _debugListSuccessHitsCurrent;
        private NativeList<int2> _debugList;
        private NativeList<int2> _debugListSuccessHits;
        private NativeHashMap<int2, int> _debugSectionMap;
        private bool _isDebugging;
        private bool _hasQuickDebuggedWholeSection;
        private int _currentSectionBeingDebugged;
        private bool _initialized;


        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DebugToggleManager>();
            _gridManagerSystemHandle = state.World.GetExistingSystem(typeof(GridManagerSystem));
            _isDebugging = true;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SortingProcess(ref state);
        }

        private void DebugLogic(ref SystemState state)
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                _isDebugging = !_isDebugging;
            }

            if (!_isDebugging)
            {
                return;
            }

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

                // Debug.Log(_debugList[_debugListCurrent]);
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

        private void SortingProcess(ref SystemState state)
        {
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var isDebug = SystemAPI.GetSingleton<DebugToggleManager>().IsDebugging;

            var walkableNodeQueue = new NativeQueue<int2>(Allocator.Temp);
            var gridSize = gridManager.Height * gridManager.Width;

            for (var i = 0; i < gridSize; i++)
            {
                var walkableCell = gridManager.WalkableGrid[i];
                if (walkableCell.IsWalkable)
                {
                    walkableNodeQueue.Enqueue(gridManager.GetXY(i));
                }
            }

            var walkablesCount = walkableNodeQueue.Count;
            if (walkablesCount <= 0)
            {
                // There's nowhere for anyone to walk
                walkableNodeQueue.Dispose();
                return;
            }

            var openNodes = new NativeParallelHashSet<int2>(walkablesCount, Allocator.Temp);
            var walkableSections =
                new NativeParallelMultiHashMap<int, WalkableSectionNode>(walkablesCount, Allocator.Temp);

            while (walkableNodeQueue.Count > 0)
            {
                openNodes.Add(walkableNodeQueue.Dequeue());
            }

            walkableNodeQueue.Dispose();

            var debugLength = isDebug ? walkablesCount : 0;
            _debugList = new NativeList<int2>(debugLength, Allocator.Persistent);
            _debugListSuccessHits = new NativeList<int2>(debugLength, Allocator.Persistent);
            _debugSectionMap = new NativeHashMap<int2, int>(debugLength, Allocator.Persistent);

            // Sort to sections
            var closedNodes = new NativeParallelHashMap<int2, WalkableSectionNode>(walkablesCount, Allocator.Temp);
            SearchCellList(walkableSections, openNodes, closedNodes, gridManager.NeighbourDeltas, isDebug);

            if (isDebug && _isDebugging)
            {
                using var sectionKeys = walkableSections.GetKeyArray(Allocator.Temp);
                var sectionCount = 0;
                foreach (var key in sectionKeys)
                {
                    if (key >= sectionCount)
                    {
                        sectionCount = key + 1;
                    }
                }

                // if (Input.GetKeyDown(KeyCode.P))
                // {
                //     Debug.Log("Section count: " + sectionCount);
                // }

                var sectionKey = 0;
                while (sectionKey < sectionCount)
                {
                    if (walkableSections.TryGetFirstValue(sectionKey, out var walkableSectionNode, out var iterator))
                    {
                        do
                        {
                            DebugDrawCell(walkableSectionNode.GridCell, GetSectionColor(sectionKey));
                        } while (walkableSections.TryGetNextValue(out walkableSectionNode, ref iterator));
                    }

                    sectionKey++;
                }
            }

            if (isDebug)
            {
                DebugLogic(ref state);
            }

            walkableSections.Dispose();
            openNodes.Dispose();
            closedNodes.Dispose();
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

            var colorIndex = sectionKey % 7;
            return colorIndex switch
            {
                0 => Color.cyan,
                1 => Color.magenta,
                2 => Color.yellow,
                3 => Color.blue,
                4 => Color.black,
                5 => Color.white,
                6 => Color.green,
                _ => Color.gray
            };
        }

        public void OnDestroy(ref SystemState state)
        {
            _debugList.Dispose();
            _debugListSuccessHits.Dispose();
            _debugSectionMap.Dispose();
        }

        private void SearchCellList(NativeParallelMultiHashMap<int, WalkableSectionNode> walkableSections,
            NativeParallelHashSet<int2> openNodes, NativeParallelHashMap<int2, WalkableSectionNode> closedNodes,
            NativeArray<int2> neighbours, bool isDebug)
        {
            var currentSection = -1;
            while (openNodes.Count() > 0)
            {
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
                walkableSections.Add(currentSection, currentNode);
                closedNodes.Add(currentCell, currentNode);
                if (isDebug)
                {
                    _debugList.Add(currentCell);
                    _debugListSuccessHits.Add(currentCell);
                    if (!_debugSectionMap.TryAdd(currentCell, currentSection))
                    {
                        Debug.Log("Try add failed (1)");
                    }
                }

                while (currentNode.NeighboursSearched < neighbours.Length)
                {
                    var neighbourCell = currentNode.GridCell + neighbours[currentNode.NeighboursSearched];
                    if (isDebug)
                    {
                        _debugList.Add(neighbourCell);
                    }

                    currentNode.NeighboursSearched++;
                    if (openNodes.Remove(neighbourCell))
                    {
                        var newNode = new WalkableSectionNode
                        {
                            GridCell = neighbourCell,
                            NodeSource = currentNode.GridCell
                        };
                        walkableSections.Add(currentSection, newNode);
                        closedNodes.TryAdd(currentNode.GridCell, currentNode);
                        currentNode = newNode;
                        if (isDebug)
                        {
                            _debugListSuccessHits.Add(neighbourCell);
                            if (!_debugSectionMap.TryAdd(neighbourCell, currentSection))
                            {
                                Debug.Log("Try add failed (2)");
                            }
                        }
                    }

                    while (currentNode.NeighboursSearched >= neighbours.Length && currentNode.NodeSource.x > -1)
                    {
                        var newNode = closedNodes[currentNode.NodeSource];
                        currentNode = newNode;
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
    }
}
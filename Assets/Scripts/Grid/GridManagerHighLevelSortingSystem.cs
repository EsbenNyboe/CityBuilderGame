using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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
        private bool _initialized;

        public void OnCreate(ref SystemState state)
        {
            _gridManagerSystemHandle = state.World.GetExistingSystem(typeof(GridManagerSystem));
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_initialized)
            {
                DebugLogic(ref state);
                return;
            }

            _initialized = true;

            SortingProcess(ref state);
        }

        private void DebugLogic(ref SystemState state)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
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

                Debug.Log(_debugList[_debugListCurrent]);
            }

            var debugPosition = new Vector3(_debugList[_debugListCurrent].x - 0.5f,
                _debugList[_debugListCurrent].y - 0.5f, 0);

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

            Debug.DrawLine(debugPosition, debugPosition + new Vector3(+1, +0), color);
            Debug.DrawLine(debugPosition, debugPosition + new Vector3(+0, +1), color);
            Debug.DrawLine(debugPosition + new Vector3(+1, +0), debugPosition + new Vector3(+1, +1), color);
            Debug.DrawLine(debugPosition + new Vector3(+0, +1), debugPosition + new Vector3(+1, +1), color);
        }

        private void SortingProcess(ref SystemState state)
        {
            _currentIteration = 0;
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);

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
            var openNodes = new NativeParallelHashSet<int2>(walkablesCount, Allocator.Temp);
            var walkableSections =
                new NativeParallelMultiHashMap<int, WalkableSectionNode>(walkablesCount, Allocator.Temp);

            Debug.Log("Length before: " + walkableNodeQueue.Count);
            var currentCell = walkableNodeQueue.Dequeue();
            while (walkableNodeQueue.Count > 0)
            {
                openNodes.Add(walkableNodeQueue.Dequeue());
            }

            walkableNodeQueue.Dispose();

            _debugList = new NativeList<int2>(walkablesCount, Allocator.Persistent);
            _debugListSuccessHits = new NativeList<int2>(walkablesCount, Allocator.Persistent);
            _debugList.Add(currentCell);
            _debugListSuccessHits.Add(currentCell);

            // Sort to sections
            var currentSection = 0;
            var currentNode = new WalkableSectionNode
            {
                GridCell = currentCell,
                NodeSource = -1
            };
            walkableSections.Add(currentSection, currentNode);
            openNodes.Remove(currentCell);
            var closedNodes = new NativeParallelHashMap<int2, WalkableSectionNode>(walkablesCount, Allocator.Temp);
            closedNodes.Add(currentCell, currentNode);

            SearchNeighbours(walkableSections, openNodes, closedNodes, currentNode, currentSection,
                gridManager.NeighbourDeltas);

            foreach (var openNode in openNodes)
            {
                Debug.Log("Open node: " + openNode);
            }

            Debug.Log("Remaining open nodes: " + openNodes.Count());
            Debug.Log("Length of final map: " + walkableSections.Count());
            walkableSections.Dispose();
            openNodes.Dispose();
            closedNodes.Dispose();
        }

        public void OnDestroy(ref SystemState state)
        {
            _debugList.Dispose();
            _debugListSuccessHits.Dispose();
        }

        private static int _currentIteration;

        private void SearchNeighbours(NativeParallelMultiHashMap<int, WalkableSectionNode> walkableSections,
            NativeParallelHashSet<int2> openNodes, NativeParallelHashMap<int2, WalkableSectionNode> closedNodes,
            WalkableSectionNode currentNode, int currentSection,
            NativeArray<int2> neighbours)
        {
            while (currentNode.NeighboursSearched < neighbours.Length)
            {
                var neighbourCell = currentNode.GridCell + neighbours[currentNode.NeighboursSearched];
                _debugList.Add(neighbourCell);
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
                    _debugListSuccessHits.Add(neighbourCell);
                }

                _currentIteration++;
                if (_currentIteration > 100000)
                {
                    Debug.LogError("Too many iterations. OpenNodes length: " + openNodes.Count());
                    return;
                }

                // how to return to NodeSource?
                if (currentNode.NeighboursSearched >= neighbours.Length && currentNode.NodeSource.x > -1)
                {
                    var newNode = closedNodes[currentNode.NodeSource];
                    currentNode = newNode;
                }
            }

            // using var enumerator = openNodes.GetEnumerator();
            // if (!enumerator.MoveNext())
            // {
            //     Debug.Log("OpenNodes is emptied");
            //     return;
            // }
            //
            // {
            //     openNodes.Remove(enumerator.Current);
            //     currentSection++;
            //     var newNode = new WalkableSectionNode
            //     {
            //         GridCell = currentNode.GridCell,
            //         NodeSource = -1
            //     };
            //     walkableSections.Add(currentSection, newNode);
            //     currentNode = newNode;
            // }
        }

        private struct WalkableSectionNode
        {
            public int2 GridCell;
            public int2 NodeSource;
            public int NeighboursSearched;
        }
    }
}
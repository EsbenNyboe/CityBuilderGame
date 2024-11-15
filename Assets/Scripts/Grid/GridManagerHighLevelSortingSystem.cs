using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Grid
{
    public partial struct GridManagerHighLevelSortingSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            _gridManagerSystemHandle = state.World.GetExistingSystem(typeof(GridManagerSystem));
        }

        public void OnUpdate(ref SystemState state)
        {
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);

            using var walkableSectionNodeQueue = new NativeQueue<WalkableSectionNode>(Allocator.Temp);
            var gridSize = gridManager.Height * gridManager.Width;

            for (var i = 0; i < gridSize; i++)
            {
                var walkableCell = gridManager.WalkableGrid[i];
                if (walkableCell.IsWalkable)
                {
                    var node = new WalkableSectionNode
                    {
                        GridIndex = i,
                        GridCell = gridManager.GetXY(i)
                    };
                    walkableSectionNodeQueue.Enqueue(node);
                }
            }

            var walkableCellCount = walkableSectionNodeQueue.Count;

            // Debug.Log("Queue count: " + walkableCellCount);
            var nativeParallelHashMapList = new NativeList<HashSetContainer>(1, Allocator.Temp);
            nativeParallelHashMapList.Add(new HashSetContainer
            {
                ParallelHashMap = new NativeParallelHashSet<int2>(walkableCellCount, Allocator.Temp)
            });

            var gridCell = walkableSectionNodeQueue.Dequeue().GridCell;
            var sectionKey = 0;
            var currentHashMap = nativeParallelHashMapList[sectionKey].ParallelHashMap;
            currentHashMap.Add(gridCell);
            AddSurroundingWalkableCellsToSection(currentHashMap, gridManager, gridCell);

            while (walkableSectionNodeQueue.Count > 0)
            {
                gridCell = walkableSectionNodeQueue.Dequeue().GridCell;
                if (currentHashMap.Contains(gridCell))
                {
                    // We're part of this section: Try to expand the current section
                    AddSurroundingWalkableCellsToSection(currentHashMap, gridManager,
                        gridCell);
                }
                else
                {
                    // Since we're not already part of this section, we'll create a new section
                    nativeParallelHashMapList.Add(new HashSetContainer
                    {
                        ParallelHashMap = new NativeParallelHashSet<int2>(walkableCellCount, Allocator.Temp)
                    });
                    sectionKey++;
                    currentHashMap = nativeParallelHashMapList[sectionKey].ParallelHashMap;
                    AddSurroundingWalkableCellsToSection(currentHashMap, gridManager, gridCell);
                    currentHashMap.Add(gridCell);
                }
            }

            var sections = nativeParallelHashMapList.Length;
            for (var i = 0; i < sections; i++)
            {
                Debug.Log("Section " + i + ": " + nativeParallelHashMapList[i].ParallelHashMap.Count());
            }
            // Debug.Log("Hashmap count: " + nativeParallelHashMap.Count());

            foreach (var hashSetContainer in nativeParallelHashMapList)
            {
                var hashSet = hashSetContainer.ParallelHashMap;
                hashSet.Dispose();
            }

            nativeParallelHashMapList.Dispose();
        }

        private void AddSurroundingWalkableCellsToSection(NativeParallelHashSet<int2> hashSet, GridManager gridManager,
            int2 gridCell)
        {
            foreach (var neighbourDelta in gridManager.NeighbourDeltas)
            {
                var neighbourCell = gridCell + neighbourDelta;
                if (gridManager.IsPositionInsideGrid(neighbourCell) && gridManager.IsWalkable(neighbourCell))
                {
                    hashSet.Add(neighbourCell);
                }
            }
        }

        private struct WalkableSectionNode
        {
            public int GridIndex;
            public int2 GridCell;
        }
    }

    public struct HashSetContainer
    {
        public NativeParallelHashSet<int2> ParallelHashMap;
    }
}
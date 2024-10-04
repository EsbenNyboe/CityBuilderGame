using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public partial class Pathfinding : SystemBase
{
    private const int MoveStraightCost = 10;
    private const int MoveDiagonalCost = 14;
    private static NativeArray<PathNode> PathNodeArrayTemplate;

    protected override void OnUpdate()
    {
        var grid = GridSetup.Instance.PathGrid;
        var gridWidth = grid.GetWidth();
        var gridHeight = grid.GetHeight();
        var gridSize = new int2(gridWidth, gridHeight);

        var findPathJobList = new List<FindPathJob>();
        // TODO: Check if this need to be disposed?
        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

        int maxPathfindingSchedulesPerFrame = 500;
        int currentAmountOfSchedules = 0;

        //if (!PathNodeArrayTemplate.IsCreated)
        //{
        //    PathNodeArrayTemplate = GetNewPathNodeArray(grid, gridSize);
        //}

        var jobLength = 0;
        var startPositions = new NativeList<int2>(Allocator.TempJob);
        var endPositions = new NativeList<int2>(Allocator.TempJob);
        var entities = new NativeList<Entity>(Allocator.TempJob);

        foreach (var (pathfindingParams, pathPositionBuffer, entity) in SystemAPI.Query<RefRO<PathfindingParams>, DynamicBuffer<PathPosition>>()
                     .WithEntityAccess())
        {
            if (currentAmountOfSchedules > maxPathfindingSchedulesPerFrame)
            {
                continue;
            }
            currentAmountOfSchedules++;

            startPositions.Add(pathfindingParams.ValueRO.StartPosition);
            endPositions.Add(pathfindingParams.ValueRO.EndPosition);
            entities.Add(entity);

            jobLength++;

            // Debug.Log("Find path");
            //var findPathJob = new FindPathJob
            //{
            //    GridSize = gridSize,
            //    PathNodeArray = GetPathNodeArray(),
            //    StartPosition = pathfindingParams.ValueRO.StartPosition,
            //    EndPosition = pathfindingParams.ValueRO.EndPosition,
            //    Entity = entity,
            //    PathFollowLookup = GetComponentLookup<PathFollow>()
            //};
            //findPathJobList.Add(findPathJob);
            //jobHandleList.Add(findPathJob.Schedule());
            // findPathJob.Run();

            entityCommandBuffer.RemoveComponent<PathfindingParams>(entity);
        }

        if (!PathNodeArrayTemplate.IsCreated)
        {
            PathNodeArrayTemplate = GetNewCombinedPathNodeArray(grid, gridSize, jobLength);
        }

        var combinedPathNodeArray = GetEmptyCombinedPathNodeArray(jobLength);
        combinedPathNodeArray.CopyFrom(PathNodeArrayTemplate);

        // MULTIPLY???? NO, THATS TOO MUCH, RIGHT?
        var pathNodeArrayLength = gridSize.x * gridSize.y;
        var indexedPathNodeArrayStart = new NativeArray<int>(jobLength, Allocator.TempJob);
        for (int i = 0; i < jobLength; i++)
        {
            indexedPathNodeArrayStart[i] = pathNodeArrayLength * i;
        }
        var indexedPathNodeArrayEnd = new NativeArray<int>(jobLength, Allocator.TempJob);
        for (int i = 0; i < jobLength; i++)
        {
            indexedPathNodeArrayEnd[i] = pathNodeArrayLength * (i + 1);
        }

        var findPathJobParallel = new FindPathJobParallel
        {
            GridSize = gridSize,
            IndexedStartPosition = startPositions.AsArray(),
            IndexedEndPosition = endPositions.AsArray(),
            CombinedPathNodeArray = combinedPathNodeArray,
            IndexedPathNodeArrayStart = indexedPathNodeArrayStart,
            IndexedPathNodeArrayEnd = indexedPathNodeArrayEnd
        };

        JobHandle handle = findPathJobParallel.Schedule(jobLength, 1);
        handle.Complete();

        for (int i = 0; i < jobLength; i++)
        {
            new SetBufferPathJob
            {
                GridSize = gridSize,
                CombinedPathNodeArray = combinedPathNodeArray,
                PathNodeArrayStart = indexedPathNodeArrayStart[i],
                Entity = entities[i],
                PathFindingParamsLookup = GetComponentLookup<PathfindingParams>(),
                PathFollowLookup = GetComponentLookup<PathFollow>(),
                PathPositionBufferLookup = GetBufferLookup<PathPosition>()
            }.Run();
        }

        //foreach (var findPathJob in findPathJobList)
        //{
        //    new SetBufferPathJob
        //    {
        //        GridSize = findPathJob.GridSize,
        //        PathNodeArray = findPathJob.PathNodeArray,
        //        Entity = findPathJob.Entity,
        //        PathFindingParamsLookup = GetComponentLookup<PathfindingParams>(),
        //        PathFollowLookup = GetComponentLookup<PathFollow>(),
        //        PathPositionBufferLookup = GetBufferLookup<PathPosition>()
        //    }.Run();
        //}

        entityCommandBuffer.Playback(EntityManager);

        if (PathNodeArrayTemplate.IsCreated)
        {
            PathNodeArrayTemplate.Dispose();
        }
        startPositions.Dispose();
        endPositions.Dispose();
        indexedPathNodeArrayStart.Dispose();
        indexedPathNodeArrayEnd.Dispose();
        combinedPathNodeArray.Dispose();
        entities.Dispose();
    }

    private NativeArray<PathNode> GetEmptyCombinedPathNodeArray(int jobLength)
    {
        var grid = GridSetup.Instance.PathGrid;
        var gridSize = new int2(grid.GetWidth(), grid.GetHeight());

        return new NativeArray<PathNode>(gridSize.x * gridSize.y * jobLength, Allocator.TempJob);
    }

    private NativeArray<PathNode> GetPathNodeArray()
    {
        var grid = GridSetup.Instance.PathGrid;
        var gridSize = new int2(grid.GetWidth(), grid.GetHeight());

        var pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.TempJob);
        pathNodeArray.CopyFrom(PathNodeArrayTemplate);

        return pathNodeArray;
    }

    private NativeArray<PathNode> GetNewCombinedPathNodeArray(Grid<GridPath> grid, int2 gridSize, int jobLength)
    {
        var combinedPathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y * jobLength, Allocator.TempJob);

        combinedPathNodeArray = FillCombinedArray(grid, gridSize, combinedPathNodeArray, jobLength);

        return combinedPathNodeArray;
    }

    private NativeArray<PathNode> GetNewPathNodeArray(Grid<GridPath> grid, int2 gridSize)
    {

        var pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.TempJob);

        pathNodeArray = FillArray(grid, gridSize, pathNodeArray);

        return pathNodeArray;
    }

    private static NativeArray<PathNode> FillCombinedArray(Grid<GridPath> grid, int2 gridSize, NativeArray<PathNode> combinedPathNodeArray, int jobLength)
    {
        for (int jobIndex = 0; jobIndex < jobLength; jobIndex++)
        {
            var pathNodeArrayStartIndex = gridSize.x * gridSize.y * jobIndex;

            for (var x = 0; x < gridSize.x; x++)
            {
                for (var y = 0; y < gridSize.y; y++)
                {
                    var pathNode = new PathNode();
                    pathNode.x = x;
                    pathNode.y = y;
                    pathNode.index = CalculateIndex(x, y, gridSize.x) + pathNodeArrayStartIndex;

                    pathNode.gCost = int.MaxValue;

                    pathNode.isWalkable = grid.GetGridObject(x, y).IsWalkable();
                    pathNode.cameFromNodeIndex = -1;

                    if (combinedPathNodeArray.Length <= pathNode.index)
                    {
                        Debug.Log("Out of range. Length: " + combinedPathNodeArray.Length + " index: " + pathNode.index);
                    }
                    combinedPathNodeArray[pathNode.index] = pathNode;
                }
            }
        }

        return combinedPathNodeArray;
    }

    private static NativeArray<PathNode> FillArray(Grid<GridPath> grid, int2 gridSize, NativeArray<PathNode> pathNodeArray)
    {
        for (var x = 0; x < gridSize.x; x++)
        {
            for (var y = 0; y < gridSize.y; y++)
            {
                var pathNode = new PathNode();
                pathNode.x = x;
                pathNode.y = y;
                pathNode.index = CalculateIndex(x, y, gridSize.x);

                pathNode.gCost = int.MaxValue;

                pathNode.isWalkable = grid.GetGridObject(x, y).IsWalkable();
                pathNode.cameFromNodeIndex = -1;

                pathNodeArray[pathNode.index] = pathNode;
            }
        }

        return pathNodeArray;
    }

    private void DebugInfo(string message)
    {
        Debug.Log(message);
    }

    private static void CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode, DynamicBuffer<PathPosition> pathPositionBuffer)
    {
        if (endNode.cameFromNodeIndex == -1)
        {
            // Couldn't find a path!
        }

        // Found a path
        pathPositionBuffer.Add(new PathPosition
        {
            Position = new int2(endNode.x, endNode.y)
        });

        var currentNode = endNode;
        while (currentNode.cameFromNodeIndex != -1)
        {
            var cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
            pathPositionBuffer.Add(new PathPosition { Position = new int2(cameFromNode.x, cameFromNode.y) });
            currentNode = cameFromNode;
        }
    }

    private NativeList<int2> CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode)
    {
        if (endNode.cameFromNodeIndex == -1)
        {
            // Couldn't find a path!
            return new NativeList<int2>(Allocator.Temp);
        }

        // Found a path
        var path = new NativeList<int2>(Allocator.Temp);
        path.Add(new int2(endNode.x, endNode.y));

        var currentNode = endNode;
        while (currentNode.cameFromNodeIndex != -1)
        {
            var cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
            path.Add(new int2(cameFromNode.x, cameFromNode.y));
            currentNode = cameFromNode;
        }

        return path;
    }

    private static bool IsPositionInsideGrid(int x, int y, int2 gridSize)
    {
        return
            x >= 0 &&
            y >= 0 &&
            x < gridSize.x &&
            y < gridSize.y;
    }

    private static int CalculateIndex(int x, int y, int gridWidth)
    {
        return x + y * gridWidth;
    }

    private static int CalculateDistanceCost(int aPosX, int aPosY, int bPosX, int bPosY)
    {
        var xDistance = math.abs(aPosX - bPosX);
        var yDistance = math.abs(aPosY - bPosY);
        var remaining = math.abs(xDistance - yDistance);
        return MoveDiagonalCost * math.min(xDistance, yDistance) + MoveStraightCost * remaining;
    }

    private static int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray)
    {
        var lowestCostPathNode = pathNodeArray[openList[0]];
        for (var i = 1; i < openList.Length; i++)
        {
            var testPathNode = pathNodeArray[openList[i]];
            if (testPathNode.fCost < lowestCostPathNode.fCost)
            {
                lowestCostPathNode = testPathNode;
            }
        }

        return lowestCostPathNode.index;
    }

    private static int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> combinedPathNodeArray, int arrayStart)
    {
        var lowestCostPathNode = combinedPathNodeArray[openList[0] + arrayStart];
        for (var i = 1; i < openList.Length; i++)
        {
            var testPathNode = combinedPathNodeArray[openList[i] + arrayStart];
            if (testPathNode.fCost < lowestCostPathNode.fCost)
            {
                lowestCostPathNode = testPathNode;
            }
        }

        return lowestCostPathNode.index;
    }

    [BurstCompile]
    private struct SetBufferPathJob : IJob
    {
        public int2 GridSize;
        public NativeArray<PathNode> CombinedPathNodeArray;

        public int PathNodeArrayStart;
        public int PathNodeArrayEnd;
        public Entity Entity;

        public ComponentLookup<PathfindingParams> PathFindingParamsLookup;
        public ComponentLookup<PathFollow> PathFollowLookup;

        public BufferLookup<PathPosition> PathPositionBufferLookup;

        public void Execute()
        {
            var pathPositionBuffer = PathPositionBufferLookup[Entity];
            pathPositionBuffer.Clear();

            var pathFindingParams = PathFindingParamsLookup[Entity];
            var endNodeIndex = CalculateIndex(pathFindingParams.EndPosition.x, pathFindingParams.EndPosition.y, GridSize.x) + PathNodeArrayStart;
            var endNode = CombinedPathNodeArray[endNodeIndex];

            if (endNode.cameFromNodeIndex == -1)
            {
                // Didn't find a path!
                // DebugInfo("Didn't find a path!");
                PathFollowLookup[Entity] = new PathFollow
                {
                    PathIndex = -1
                };
            }
            else
            {
                // Found a path
                CalculatePath(CombinedPathNodeArray, endNode, pathPositionBuffer);
                PathFollowLookup[Entity] = new PathFollow
                {
                    PathIndex = pathPositionBuffer.Length - 1
                };
            }
        }
    }

    private struct FindPathJobParallel : IJobParallelFor
    {
        [ReadOnly]
        public int2 GridSize;
        // TODO: Replace IndexedPathNodeArrayStart/End with simple "length" int? 
        [ReadOnly]
        public NativeArray<int> IndexedPathNodeArrayStart;
        [ReadOnly]
        public NativeArray<int> IndexedPathNodeArrayEnd;
        [ReadOnly]
        public NativeArray<int2> IndexedStartPosition;
        [ReadOnly]
        public NativeArray<int2> IndexedEndPosition;

        [NativeDisableParallelForRestriction]
        public NativeArray<PathNode> CombinedPathNodeArray;

        public void Execute(int index)
        {
            for (var i = IndexedPathNodeArrayStart[index]; i < IndexedPathNodeArrayEnd[index]; i++)
            {
                var pathNode = CombinedPathNodeArray[i];
                pathNode.hCost = CalculateDistanceCost(pathNode.x, pathNode.y, IndexedEndPosition[index].x, IndexedEndPosition[index].y);
                pathNode.cameFromNodeIndex = -1;

                CombinedPathNodeArray[i] = pathNode;
            }

            var neighbourOffsetArrayX = new NativeArray<int>(8, Allocator.Temp);
            neighbourOffsetArrayX[0] = -1; // Left
            neighbourOffsetArrayX[1] = +1; // Right
            neighbourOffsetArrayX[2] = 0; // Up
            neighbourOffsetArrayX[3] = 0; // Down
            neighbourOffsetArrayX[4] = -1; // Left Down
            neighbourOffsetArrayX[5] = -1; // Left Up
            neighbourOffsetArrayX[6] = +1; // Right Down
            neighbourOffsetArrayX[7] = +1; // Right Up
            var neighbourOffsetArrayY = new NativeArray<int>(8, Allocator.Temp);
            neighbourOffsetArrayY[0] = 0; // Left
            neighbourOffsetArrayY[1] = 0; // Right
            neighbourOffsetArrayY[2] = +1; // Up
            neighbourOffsetArrayY[3] = -1; // Down
            neighbourOffsetArrayY[4] = -1; // Left Down
            neighbourOffsetArrayY[5] = +1; // Left Up
            neighbourOffsetArrayY[6] = -1; // Right Down
            neighbourOffsetArrayY[7] = +1; // Right Up

            var endNodeIndex = CalculateIndex(IndexedEndPosition[index].x, IndexedEndPosition[index].y, GridSize.x) + IndexedPathNodeArrayStart[index];
            var startNodeIndex = CalculateIndex(IndexedStartPosition[index].x, IndexedStartPosition[index].y, GridSize.x) + IndexedPathNodeArrayStart[index];

            var startNode = CombinedPathNodeArray[startNodeIndex];
            startNode.gCost = 0;
            startNode.CalculateFCost();
            CombinedPathNodeArray[startNode.index] = startNode;

            var openList = new NativeList<int>(Allocator.Temp);
            var closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.index);

            while (openList.Length > 0)
            {
                var currentNodeIndex = GetLowestCostFNodeIndex(openList, CombinedPathNodeArray);
                var currentNode = CombinedPathNodeArray[currentNodeIndex];

                if (currentNodeIndex == endNodeIndex)
                {
                    // Reached our destination!
                    break;
                }

                // Remove current node from Open List
                for (var i = 0; i < openList.Length; i++)
                {
                    if (openList[i] == currentNodeIndex)
                    {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }

                closedList.Add(currentNodeIndex);

                for (var i = 0; i < neighbourOffsetArrayX.Length; i++)
                {
                    var neighbourPosX = currentNode.x + neighbourOffsetArrayX[i];
                    var neighbourPosY = currentNode.y + neighbourOffsetArrayY[i];

                    if (!IsPositionInsideGrid(neighbourPosX, neighbourPosY, GridSize))
                    {
                        // Neighbour not valid position
                        continue;
                    }

                    var neighbourNodeIndex = CalculateIndex(neighbourPosX, neighbourPosY, GridSize.x) + IndexedPathNodeArrayStart[index];

                    if (closedList.Contains(neighbourNodeIndex))
                    {
                        // Already searched this node
                        continue;
                    }

                    var neighbourNode = CombinedPathNodeArray[neighbourNodeIndex];
                    if (!neighbourNode.isWalkable)
                    {
                        // Not walkable
                        continue;
                    }

                    var tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode.x, currentNode.y, neighbourPosX, neighbourPosY);
                    if (tentativeGCost < neighbourNode.gCost)
                    {
                        neighbourNode.cameFromNodeIndex = currentNodeIndex;
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.CalculateFCost();
                        CombinedPathNodeArray[neighbourNodeIndex] = neighbourNode;

                        if (!openList.Contains(neighbourNode.index))
                        {
                            openList.Add(neighbourNode.index);
                        }
                    }
                }
            }

            neighbourOffsetArrayX.Dispose();
            neighbourOffsetArrayY.Dispose();
            openList.Dispose();
            closedList.Dispose();
        }
    }

    [BurstCompile]
    private struct FindPathJob : IJob
    {
        public int2 GridSize;
        public NativeArray<PathNode> PathNodeArray;

        public int2 StartPosition;
        public int2 EndPosition;

        public Entity Entity;

        [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<PathFollow> PathFollowLookup;

        public void Execute()
        {
            for (var i = 0; i < PathNodeArray.Length; i++)
            {
                var pathNode = PathNodeArray[i];
                pathNode.hCost = CalculateDistanceCost(pathNode.x, pathNode.y, EndPosition.x, EndPosition.y);
                pathNode.cameFromNodeIndex = -1;

                PathNodeArray[i] = pathNode;
            }

            var neighbourOffsetArrayX = new NativeArray<int>(8, Allocator.Temp);
            neighbourOffsetArrayX[0] = -1; // Left
            neighbourOffsetArrayX[1] = +1; // Right
            neighbourOffsetArrayX[2] = 0; // Up
            neighbourOffsetArrayX[3] = 0; // Down
            neighbourOffsetArrayX[4] = -1; // Left Down
            neighbourOffsetArrayX[5] = -1; // Left Up
            neighbourOffsetArrayX[6] = +1; // Right Down
            neighbourOffsetArrayX[7] = +1; // Right Up
            var neighbourOffsetArrayY = new NativeArray<int>(8, Allocator.Temp);
            neighbourOffsetArrayY[0] = 0; // Left
            neighbourOffsetArrayY[1] = 0; // Right
            neighbourOffsetArrayY[2] = +1; // Up
            neighbourOffsetArrayY[3] = -1; // Down
            neighbourOffsetArrayY[4] = -1; // Left Down
            neighbourOffsetArrayY[5] = +1; // Left Up
            neighbourOffsetArrayY[6] = -1; // Right Down
            neighbourOffsetArrayY[7] = +1; // Right Up

            var endNodeIndex = CalculateIndex(EndPosition.x, EndPosition.y, GridSize.x);

            var startNode = PathNodeArray[CalculateIndex(StartPosition.x, StartPosition.y, GridSize.x)];
            startNode.gCost = 0;
            startNode.CalculateFCost();
            PathNodeArray[startNode.index] = startNode;

            var openList = new NativeList<int>(Allocator.Temp);
            var closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.index);

            while (openList.Length > 0)
            {
                var currentNodeIndex = GetLowestCostFNodeIndex(openList, PathNodeArray);
                var currentNode = PathNodeArray[currentNodeIndex];

                if (currentNodeIndex == endNodeIndex)
                {
                    // Reached our destination!
                    break;
                }

                // Remove current node from Open List
                for (var i = 0; i < openList.Length; i++)
                {
                    if (openList[i] == currentNodeIndex)
                    {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }

                closedList.Add(currentNodeIndex);

                for (var i = 0; i < neighbourOffsetArrayX.Length; i++)
                {
                    var neighbourPosX = currentNode.x + neighbourOffsetArrayX[i];
                    var neighbourPosY = currentNode.y + neighbourOffsetArrayY[i];

                    if (!IsPositionInsideGrid(neighbourPosX, neighbourPosY, GridSize))
                    {
                        // Neighbour not valid position
                        continue;
                    }

                    var neighbourNodeIndex = CalculateIndex(neighbourPosX, neighbourPosY, GridSize.x);

                    if (closedList.Contains(neighbourNodeIndex))
                    {
                        // Already searched this node
                        continue;
                    }

                    var neighbourNode = PathNodeArray[neighbourNodeIndex];
                    if (!neighbourNode.isWalkable)
                    {
                        // Not walkable
                        continue;
                    }

                    var tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode.x, currentNode.y, neighbourPosX, neighbourPosY);
                    if (tentativeGCost < neighbourNode.gCost)
                    {
                        neighbourNode.cameFromNodeIndex = currentNodeIndex;
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.CalculateFCost();
                        PathNodeArray[neighbourNodeIndex] = neighbourNode;

                        if (!openList.Contains(neighbourNode.index))
                        {
                            openList.Add(neighbourNode.index);
                        }
                    }
                }
            }

            neighbourOffsetArrayX.Dispose();
            neighbourOffsetArrayY.Dispose();
            openList.Dispose();
            closedList.Dispose();
        }
    }
}

public struct PathNode
{
    public int x;
    public int y;

    public int index;

    public int gCost;
    public int hCost;
    public int fCost;

    public bool isWalkable;

    public int cameFromNodeIndex;

    public void CalculateFCost()
    {
        fCost = gCost + hCost;
    }

    public void SetIsWalkable(bool isWalkable)
    {
        this.isWalkable = isWalkable;
    }
}
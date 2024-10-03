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
        var jobHandleList = new NativeList<JobHandle>(Allocator.Temp);
        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

        int maxPathfindingSchedulesPerFrame = 30;
        int currentAmountOfSchedules = 0;

        if (!PathNodeArrayTemplate.IsCreated)
        {
            PathNodeArrayTemplate = GetNewPathNodeArray(grid, gridSize);
        }

        foreach (var (pathfindingParams, pathPositionBuffer, entity) in SystemAPI.Query<RefRO<PathfindingParams>, DynamicBuffer<PathPosition>>()
                     .WithEntityAccess())
        {
            if (currentAmountOfSchedules > maxPathfindingSchedulesPerFrame)
            {
                continue;
            }
            currentAmountOfSchedules++;

            // Debug.Log("Find path");
            var findPathJob = new FindPathJob
            {
                GridSize = gridSize,
                PathNodeArray = GetPathNodeArray(),
                StartPosition = pathfindingParams.ValueRO.StartPosition,
                EndPosition = pathfindingParams.ValueRO.EndPosition,
                Entity = entity,
                PathFollowLookup = GetComponentLookup<PathFollow>()
            };
            findPathJobList.Add(findPathJob);
            jobHandleList.Add(findPathJob.Schedule());
            // findPathJob.Run();

            entityCommandBuffer.RemoveComponent<PathfindingParams>(entity);
        }

        JobHandle.CompleteAll(jobHandleList.AsArray());

        foreach (var findPathJob in findPathJobList)
        {
            new SetBufferPathJob
            {
                GridSize = findPathJob.GridSize,
                PathNodeArray = findPathJob.PathNodeArray,
                Entity = findPathJob.Entity,
                PathFindingParamsLookup = GetComponentLookup<PathfindingParams>(),
                PathFollowLookup = GetComponentLookup<PathFollow>(),
                PathPositionBufferLookup = GetBufferLookup<PathPosition>()
            }.Run();
        }

        if (PathNodeArrayTemplate.IsCreated)
        {
            PathNodeArrayTemplate.Dispose();
        }
        entityCommandBuffer.Playback(EntityManager);
    }

    private NativeArray<PathNode> GetPathNodeArray()
    {
        var grid = GridSetup.Instance.PathGrid;
        var gridSize = new int2(grid.GetWidth(), grid.GetHeight());

        var pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.TempJob);
        pathNodeArray.CopyFrom(PathNodeArrayTemplate);

        return pathNodeArray;
    }

    private NativeArray<PathNode> GetNewPathNodeArray(Grid<GridPath> grid, int2 gridSize)
    {

        var pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.TempJob);

        pathNodeArray = FillArray(grid, gridSize, pathNodeArray);

        return pathNodeArray;
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

    [BurstCompile]
    private struct SetBufferPathJob : IJob
    {
        public int2 GridSize;
        [DeallocateOnJobCompletion] public NativeArray<PathNode> PathNodeArray;

        public Entity Entity;

        public ComponentLookup<PathfindingParams> PathFindingParamsLookup;
        public ComponentLookup<PathFollow> PathFollowLookup;

        public BufferLookup<PathPosition> PathPositionBufferLookup;

        public void Execute()
        {
            var pathPositionBuffer = PathPositionBufferLookup[Entity];
            pathPositionBuffer.Clear();

            var pathFindingParams = PathFindingParamsLookup[Entity];
            var endNodeIndex = CalculateIndex(pathFindingParams.EndPosition.x, pathFindingParams.EndPosition.y, GridSize.x);
            var endNode = PathNodeArray[endNodeIndex];

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
                CalculatePath(PathNodeArray, endNode, pathPositionBuffer);
                PathFollowLookup[Entity] = new PathFollow
                {
                    PathIndex = pathPositionBuffer.Length - 1
                };
            }
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

            var neighbourOffsetArray = new NativeArray<int2>(8, Allocator.Temp);
            neighbourOffsetArray[0] = new int2(-1, 0); // Left
            neighbourOffsetArray[1] = new int2(+1, 0); // Right
            neighbourOffsetArray[2] = new int2(0, +1); // Up
            neighbourOffsetArray[3] = new int2(0, -1); // Down
            neighbourOffsetArray[4] = new int2(-1, -1); // Left Down
            neighbourOffsetArray[5] = new int2(-1, +1); // Left Up
            neighbourOffsetArray[6] = new int2(+1, -1); // Right Down
            neighbourOffsetArray[7] = new int2(+1, +1); // Right Up

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

                for (var i = 0; i < neighbourOffsetArray.Length; i++)
                {
                    var neighbourOffset = neighbourOffsetArray[i];
                    var neighbourPosX = currentNode.x + neighbourOffset.x;
                    var neighbourPosY = currentNode.y + neighbourOffset.y;

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

            neighbourOffsetArray.Dispose();
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
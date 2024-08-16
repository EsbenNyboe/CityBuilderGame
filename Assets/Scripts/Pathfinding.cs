/*
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public partial class Pathfinding : SystemBase
{
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    protected override void OnUpdate()
    {
        var gridWidth = PathfindingGridSetup.Instance.pathfindingGrid.GetWidth();
        var gridHeight = PathfindingGridSetup.Instance.pathfindingGrid.GetHeight();
        var gridSize = new int2(gridWidth, gridHeight);

        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);
        foreach (var (pathfindingParams, pathPositionBuffer, entity) in SystemAPI.Query<RefRO<PathfindingParams>, DynamicBuffer<PathPosition>>()
                     .WithEntityAccess())
        {
            Debug.Log("Find path");
            var findPathJob = new FindPathJob
            {
                GridSize = gridSize,
                PathNodeArray = GetPathNodeArray(),
                StartPosition = pathfindingParams.ValueRO.StartPosition,
                EndPosition = pathfindingParams.ValueRO.EndPosition,
                Entity = entity,
                PathFollowLookup = GetComponentLookup<PathFollow>(),
                PathPositionBuffer = pathPositionBuffer
            };
            findPathJob.Run();

            entityCommandBuffer.RemoveComponent<PathfindingParams>(entity);
        }

        entityCommandBuffer.Playback(EntityManager);
    }

    private NativeArray<PathNode> GetPathNodeArray()
    {
        var grid = PathfindingGridSetup.Instance.pathfindingGrid;
        var gridSize = new int2(grid.GetWidth(), grid.GetHeight());

        var pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.TempJob);

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

    // [BurstDiscard]
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

    private static bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize)
    {
        return
            gridPosition.x >= 0 &&
            gridPosition.y >= 0 &&
            gridPosition.x < gridSize.x &&
            gridPosition.y < gridSize.y;
    }

    private static int CalculateIndex(int x, int y, int gridWidth)
    {
        return x + y * gridWidth;
    }

    private static int CalculateDistanceCost(int2 aPosition, int2 bPosition)
    {
        var xDistance = math.abs(aPosition.x - bPosition.x);
        var yDistance = math.abs(aPosition.y - bPosition.y);
        var remaining = math.abs(xDistance - yDistance);
        return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
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

    // [BurstCompile]
    private struct FindPathJob : IJob
    {
        public int2 GridSize;
        [DeallocateOnJobCompletion] public NativeArray<PathNode> PathNodeArray;

        public int2 StartPosition;
        public int2 EndPosition;

        public Entity Entity;
        public ComponentLookup<PathFollow> PathFollowLookup;
        public DynamicBuffer<PathPosition> PathPositionBuffer;

        public void Execute()
        {
            for (var i = 0; i < PathNodeArray.Length; i++)
            {
                var pathNode = PathNodeArray[i];
                pathNode.hCost = CalculateDistanceCost(new int2(pathNode.x, pathNode.y), EndPosition);
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
                    var neighbourPosition = new int2(currentNode.x + neighbourOffset.x, currentNode.y + neighbourOffset.y);

                    if (!IsPositionInsideGrid(neighbourPosition, GridSize))
                    {
                        // Neighbour not valid position
                        continue;
                    }

                    var neighbourNodeIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y, GridSize.x);

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

                    var currentNodePosition = new int2(currentNode.x, currentNode.y);

                    var tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNodePosition, neighbourPosition);
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

            PathPositionBuffer.Clear();
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
                CalculatePath(PathNodeArray, endNode, PathPositionBuffer);
                PathFollowLookup[Entity] = new PathFollow
                {
                    PathIndex = PathPositionBuffer.Length - 1
                };
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
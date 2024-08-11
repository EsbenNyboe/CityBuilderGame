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
        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);
        foreach (var (pathfindingParams, pathPositionBuffer, entity) in SystemAPI.Query<RefRO<PathfindingParams>, DynamicBuffer<PathPosition>>()
                     .WithEntityAccess())
        {
            Debug.Log("Find path");
            var findPathJob = new FindPathJob
            {
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

    // [BurstCompile]
    private struct FindPathJob : IJob
    {
        public int2 StartPosition;
        public int2 EndPosition;

        public Entity Entity;
        public ComponentLookup<PathFollow> PathFollowLookup;
        public DynamicBuffer<PathPosition> PathPositionBuffer;

        public void Execute()
        {
            var gridSize = new int2(100, 100);

            var pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.Temp);

            for (var x = 0; x < gridSize.x; x++)
            {
                for (var y = 0; y < gridSize.y; y++)
                {
                    var pathNode = new PathNode();
                    pathNode.x = x;
                    pathNode.y = y;
                    pathNode.index = CalculateIndex(x, y, gridSize.x);

                    pathNode.gCost = int.MaxValue;
                    pathNode.hCost = CalculateDistanceCost(new int2(x, y), EndPosition);
                    pathNode.CalculateFCost();

                    pathNode.isWalkable = true;
                    pathNode.cameFromNodeIndex = -1;

                    pathNodeArray[pathNode.index] = pathNode;
                }
            }

            // Place Testing Walls
            {
                var walkablePathNode = pathNodeArray[CalculateIndex(1, 0, gridSize.x)];
                walkablePathNode.SetIsWalkable(false);
                pathNodeArray[CalculateIndex(1, 0, gridSize.x)] = walkablePathNode;

                walkablePathNode = pathNodeArray[CalculateIndex(1, 1, gridSize.x)];
                walkablePathNode.SetIsWalkable(false);
                pathNodeArray[CalculateIndex(1, 1, gridSize.x)] = walkablePathNode;

                walkablePathNode = pathNodeArray[CalculateIndex(1, 2, gridSize.x)];
                walkablePathNode.SetIsWalkable(false);
                pathNodeArray[CalculateIndex(1, 2, gridSize.x)] = walkablePathNode;
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

            var endNodeIndex = CalculateIndex(EndPosition.x, EndPosition.y, gridSize.x);

            var startNode = pathNodeArray[CalculateIndex(StartPosition.x, StartPosition.y, gridSize.x)];
            startNode.gCost = 0;
            startNode.CalculateFCost();
            pathNodeArray[startNode.index] = startNode;

            var openList = new NativeList<int>(Allocator.Temp);
            var closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.index);

            while (openList.Length > 0)
            {
                var currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
                var currentNode = pathNodeArray[currentNodeIndex];

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

                    if (!IsPositionInsideGrid(neighbourPosition, gridSize))
                    {
                        // Neighbour not valid position
                        continue;
                    }

                    var neighbourNodeIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y, gridSize.x);

                    if (closedList.Contains(neighbourNodeIndex))
                    {
                        // Already searched this node
                        continue;
                    }

                    var neighbourNode = pathNodeArray[neighbourNodeIndex];
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
                        pathNodeArray[neighbourNodeIndex] = neighbourNode;

                        if (!openList.Contains(neighbourNode.index))
                        {
                            openList.Add(neighbourNode.index);
                        }
                    }
                }
            }

            PathPositionBuffer.Clear();
            var endNode = pathNodeArray[endNodeIndex];
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
                CalculatePath(pathNodeArray, endNode, PathPositionBuffer);
                PathFollowLookup[Entity] = new PathFollow
                {
                    PathIndex = PathPositionBuffer.Length - 1
                };
            }

            pathNodeArray.Dispose();
            neighbourOffsetArray.Dispose();
            openList.Dispose();
            closedList.Dispose();
        }

        // [BurstDiscard]
        private void DebugInfo(string message)
        {
            Debug.Log(message);
        }

        private void CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode, DynamicBuffer<PathPosition> pathPositionBuffer)
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

        private bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize)
        {
            return
                gridPosition.x >= 0 &&
                gridPosition.y >= 0 &&
                gridPosition.x < gridSize.x &&
                gridPosition.y < gridSize.y;
        }

        private int CalculateIndex(int x, int y, int gridWidth)
        {
            return x + y * gridWidth;
        }

        private int CalculateDistanceCost(int2 aPosition, int2 bPosition)
        {
            var xDistance = math.abs(aPosition.x - bPosition.x);
            var yDistance = math.abs(aPosition.y - bPosition.y);
            var remaining = math.abs(xDistance - yDistance);
            return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
        }


        private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray)
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

        private struct PathNode
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
    }
}
/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using CodeMonkey.Utils;

/*
public class Pathfinding : ComponentSystem {

    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    protected override void OnUpdate() {
        List<FindPathJob> findPathJobList = new List<FindPathJob>();

        NativeList<JobHandle> jobHandleList = new NativeList<JobHandle>(Allocator.TempJob);
        
        Grid<GridNode> grid = PathfindingGridSetup.Instance.pathfindingGrid;
        int2 gridSize = new int2(grid.GetWidth(), grid.GetHeight());
        
        NativeArray<PathNode> pathNodeArray = GetPathNodeArray();

        ComponentDataFromEntity<PathfindingParams> pathfindingParamsEntities = GetComponentDataFromEntity<PathfindingParams>();

        Entities.WithAll<PathfindingParams>().ForEach((Entity entity) => {
            NativeArray<PathNode> tmpPathNodeArray = new NativeArray<PathNode>(pathNodeArray, Allocator.TempJob);

            FindPathJob findPathJob = new FindPathJob {
                gridSize = gridSize,
                pathNodeArray = tmpPathNodeArray,
                pathfindingParamsEntities = pathfindingParamsEntities,
                entity = entity,
            };
            findPathJobList.Add(findPathJob);

            PostUpdateCommands.RemoveComponent<PathfindingParams>(entity);
        });

        foreach (FindPathJob findPathJob in findPathJobList) {
            jobHandleList.Add(findPathJob.Schedule());
        }

        JobHandle.CompleteAll(jobHandleList);
        

        foreach (FindPathJob findPathJob in findPathJobList) {
            new SetBufferPathJob {
                gridSize = gridSize,
                pathNodeArray = findPathJob.pathNodeArray,
                entity = findPathJob.entity,
                pathfindingParamsEntities = GetComponentDataFromEntity<PathfindingParams>(),
                pathFollowComponentDataFromEntity = GetComponentDataFromEntity<PathFollow>(),
                pathPositionBufferFromEntity = GetBufferFromEntity<PathPosition>(),
            }.Run();
        }

        pathNodeArray.Dispose();
        jobHandleList.Dispose();
    }

    private NativeArray<PathNode> GetPathNodeArray() {
        Grid<GridNode> grid = PathfindingGridSetup.Instance.pathfindingGrid;

        int2 gridSize = new int2(grid.GetWidth(), grid.GetHeight());
        NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.TempJob);

        for (int x = 0; x < grid.GetWidth(); x++) {
            for (int y = 0; y < grid.GetHeight(); y++) {
                PathNode pathNode = new PathNode();
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


    [BurstCompile]
    private struct SetBufferPathJob : IJob {
        
        public int2 gridSize;

        [DeallocateOnJobCompletion]
        public NativeArray<PathNode> pathNodeArray;

        public Entity entity;

        public ComponentDataFromEntity<PathfindingParams> pathfindingParamsEntities;
        public ComponentDataFromEntity<PathFollow> pathFollowComponentDataFromEntity;
        public BufferFromEntity<PathPosition> pathPositionBufferFromEntity;

        public void Execute() {
            DynamicBuffer<PathPosition> pathPositionBuffer = pathPositionBufferFromEntity[entity];
            pathPositionBuffer.Clear();

            PathfindingParams pathfindingParams = pathfindingParamsEntities[entity];
            int endNodeIndex = CalculateIndex(pathfindingParams.endPosition.x, pathfindingParams.endPosition.y, gridSize.x);
            PathNode endNode = pathNodeArray[endNodeIndex];
            if (endNode.cameFromNodeIndex == -1) {
                // Didn't find a path!
                //Debug.Log("Didn't find a path!");
                pathFollowComponentDataFromEntity[entity] = new PathFollow { pathIndex = -1 };
            } else {
                // Found a path
                GeneratePath(pathNodeArray, endNode, pathPositionBuffer);
                
                pathFollowComponentDataFromEntity[entity] = new PathFollow { pathIndex = pathPositionBuffer.Length - 1 };
            }

        }
    }

    [BurstCompile]
    private struct FindPathJob : IJob {

        public int2 gridSize;

        [NativeDisableParallelForRestriction]
        public NativeArray<PathNode> pathNodeArray;

        [NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<PathfindingParams> pathfindingParamsEntities;

        public Entity entity;

        //[NativeDisableParallelForRestriction, NativeDisableContainerSafetyRestriction, NativeDisableUnsafePtrRestriction, WriteOnly]
        //public BufferFromEntity<PathPosition> pathPositionBufferFromEntity;


        public void Execute() {
            PathfindingParams pathfindingParams = pathfindingParamsEntities[entity];
            Execute(entity, pathfindingParams.startPosition, pathfindingParams.endPosition);
        }


        public void Execute(Entity entity, int2 startPosition, int2 endPosition) {
            for (int i = 0; i < pathNodeArray.Length; i++) {
                PathNode pathNode = pathNodeArray[i];
                pathNode.hCost = CalculateDistanceCost(new int2(pathNode.x, pathNode.y), endPosition);
                pathNode.CalculateFCost();
                pathNode.cameFromNodeIndex = -1;

                pathNodeArray[i] = pathNode;
            }

            NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(8, Allocator.Temp);
            neighbourOffsetArray[0] = new int2(-1, 0); // Left
            neighbourOffsetArray[1] = new int2(+1, 0); // Right
            neighbourOffsetArray[2] = new int2(0, +1); // Up
            neighbourOffsetArray[3] = new int2(0, -1); // Down
            neighbourOffsetArray[4] = new int2(-1, -1); // Left Down
            neighbourOffsetArray[5] = new int2(-1, +1); // Left Up
            neighbourOffsetArray[6] = new int2(+1, -1); // Right Down
            neighbourOffsetArray[7] = new int2(+1, +1); // Right Up

            int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, gridSize.x);

            PathNode startNode = pathNodeArray[CalculateIndex(startPosition.x, startPosition.y, gridSize.x)];
            startNode.gCost = 0;
            startNode.CalculateFCost();
            pathNodeArray[startNode.index] = startNode;

            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.index);

            while (openList.Length > 0) {
                int currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
                PathNode currentNode = pathNodeArray[currentNodeIndex];

                if (currentNodeIndex == endNodeIndex) {
                    // Reached our destination!
                    break;
                }

                // Remove current node from Open List
                for (int i = 0; i < openList.Length; i++) {
                    if (openList[i] == currentNodeIndex) {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }

                closedList.Add(currentNodeIndex);

                for (int i = 0; i < neighbourOffsetArray.Length; i++) {
                    int2 neighbourOffset = neighbourOffsetArray[i];
                    int2 neighbourPosition = new int2(currentNode.x + neighbourOffset.x, currentNode.y + neighbourOffset.y);

                    if (!IsPositionInsideGrid(neighbourPosition, gridSize)) {
                        // Neighbour not valid position
                        continue;
                    }

                    int neighbourNodeIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y, gridSize.x);

                    if (closedList.Contains(neighbourNodeIndex)) {
                        // Already searched this node
                        continue;
                    }

                    PathNode neighbourNode = pathNodeArray[neighbourNodeIndex];
                    if (!neighbourNode.isWalkable) {
                        // Not walkable
                        continue;
                    }

                    int2 currentNodePosition = new int2(currentNode.x, currentNode.y);

	                int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNodePosition, neighbourPosition);
	                if (tentativeGCost < neighbourNode.gCost) {
		                neighbourNode.cameFromNodeIndex = currentNodeIndex;
		                neighbourNode.gCost = tentativeGCost;
		                neighbourNode.CalculateFCost();
		                pathNodeArray[neighbourNodeIndex] = neighbourNode;

		                if (!openList.Contains(neighbourNode.index)) {
			                openList.Add(neighbourNode.index);
		                }
	                }

                }
            }

            /*
            DynamicBuffer<PathPosition> pathPositionBuffer = pathPositionBufferFromEntity[entity];
            pathPositionBuffer.Clear();

            PathNode endNode = pathNodeArray[endNodeIndex];
            if (endNode.cameFromNodeIndex == -1) {
                // Didn't find a path!
                //Debug.Log("Didn't find a path!");
                pathFollowComponentDataFromEntity[entity] = new PathFollow { pathIndex = -1 };
            } else {
                // Found a path
                GeneratePath(pathNodeArray, endNode, pathPositionBuffer);
                
                pathFollowComponentDataFromEntity[entity] = new PathFollow { pathIndex = pathPositionBuffer.Length - 1 };
            }
            *

            neighbourOffsetArray.Dispose();
            openList.Dispose();
            closedList.Dispose();
        }
    }
    
    
         
    private static void GeneratePath(NativeArray<PathNode> pathNodeArray, PathNode endNode, DynamicBuffer<PathPosition> pathPositionBuffer) {
        if (endNode.cameFromNodeIndex == -1) {
            // Couldn't find a path!
        } else {
            // Found a path
            pathPositionBuffer.Add(new PathPosition { position = new int2(endNode.x, endNode.y) });

            PathNode currentNode = endNode;
            while (currentNode.cameFromNodeIndex != -1) {
                PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                pathPositionBuffer.Add(new PathPosition { position = new int2(cameFromNode.x, cameFromNode.y) });
                currentNode = cameFromNode;
            }
        }
    }

    private static NativeList<int2> CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode) {
        if (endNode.cameFromNodeIndex == -1) {
            // Couldn't find a path!
            return new NativeList<int2>(Allocator.Temp);
        } else {
            // Found a path
            NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
            path.Add(new int2(endNode.x, endNode.y));

            PathNode currentNode = endNode;
            while (currentNode.cameFromNodeIndex != -1) {
                PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                path.Add(new int2(cameFromNode.x, cameFromNode.y));
                currentNode = cameFromNode;
            }

            return path;
        }
    }

    private static bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize) {
        return
            gridPosition.x >= 0 && 
            gridPosition.y >= 0 &&
            gridPosition.x < gridSize.x &&
            gridPosition.y < gridSize.y;
    }

    private static int CalculateIndex(int x, int y, int gridWidth) {
        return x + y * gridWidth;
    }

    private static int CalculateDistanceCost(int2 aPosition, int2 bPosition) {
        int xDistance = math.abs(aPosition.x - bPosition.x);
        int yDistance = math.abs(aPosition.y - bPosition.y);
        int remaining = math.abs(xDistance - yDistance);
        return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    }

    
    private static int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray) {
        PathNode lowestCostPathNode = pathNodeArray[openList[0]];
        for (int i = 1; i < openList.Length; i++) {
            PathNode testPathNode = pathNodeArray[openList[i]];
            if (testPathNode.fCost < lowestCostPathNode.fCost) {
                lowestCostPathNode = testPathNode;
            }
        }
        return lowestCostPathNode.index;
    }

    public struct PathNode {
        public int x;
        public int y;

        public int index;

        public int gCost;
        public int hCost;
        public int fCost;

        public bool isWalkable;

        public int cameFromNodeIndex;

        public void CalculateFCost() {
            fCost = gCost + hCost;
        }

        public void SetIsWalkable(bool isWalkable) {
            this.isWalkable = isWalkable;
        }
    }

}
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using CodeMonkey.Utils;

/*
public class Pathfinding : MonoBehaviour {

    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    private void Start() {
        //FindPath(new int2(0, 0), new int2(3, 1));
        //*
        FunctionPeriodic.Create(() => {
            Profiler.BeginSample("Pathfinding");
            float startTime = Time.realtimeSinceStartup;
            int jobCount = 10;
            NativeArray<JobHandle> jobHandleArray = new NativeArray<JobHandle>(jobCount, Allocator.Temp);
            FindPathJob[] findPathJobArray = new FindPathJob[jobCount];
            for (int i = 0; i < jobCount; i++) {
                FindPathJob findPathJob = new FindPathJob { startPosition = new int2(0, 0), endPosition = new int2(99, 99), path = new NativeList<PathNode>(Allocator.TempJob) };
                JobHandle jobHandle = findPathJob.Schedule();
                findPathJobArray[i] = findPathJob;
                jobHandleArray[i] = jobHandle;

                /*
                if (findPathJob.path.Length == 0) {
                    // Couldn't find path
                    Debug.Log("Couldn't find path");
                } else {
                    // Found path
                    foreach (PathNode pathNode in findPathJob.path) {
                        //Debug.Log(pathNode);
                    }
                    //Debug.Log(findPathJob.path.Length);
                }
                *
                //findPathJob.path.Dispose();
            }
            JobHandle.CompleteAll(jobHandleArray);
            for (int i = 0; i < jobCount; i++) {
                findPathJobArray[i].path.Dispose();
            }
            jobHandleArray.Dispose();
            Debug.Log("Time: " + ((Time.realtimeSinceStartup  - startTime) * 1000f));
            Profiler.EndSample();
        }, .2f);
        //*
    }

    #region FindPath
    private void FindPath(int2 startPosition, int2 endPosition) {
        int2 gridSize = new int2(4, 2);
        
        NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.Temp);
        NativeMultiHashMap<int, int2> pathNodeNeighboursMultiHashMap = new NativeMultiHashMap<int, int2>(gridSize.x * gridSize.y, Allocator.Temp);

        for (int x = 0; x < gridSize.x; x++) {
            for (int y = 0; y < gridSize.y; y++) {
                int index = CalculateIndex(x, y, gridSize.x);
                PathNode pathNode = new PathNode { index = index, x = x, y = y };
                pathNode.gCost = int.MaxValue;
                pathNode.CalculateFCost();
                //pathNode.CalculateNeighbours(gridSize);
                pathNode.cameFromNodeIndex = -1;
                pathNodeArray[index] = pathNode;

                NativeList<int2> neighbourList = GetNeighbours(new int2(x, y), gridSize);
                for (int i = 0; i < neighbourList.Length; i++) {
                    pathNodeNeighboursMultiHashMap.Add(index, neighbourList[i]);
                }
                neighbourList.Dispose();
                
            }
        }

        int startIndex = CalculateIndex(startPosition.x, startPosition.y, gridSize.x);
        int endIndex = CalculateIndex(endPosition.x, endPosition.y, gridSize.x);

        PathNode startNode = pathNodeArray[startIndex];
        PathNode endNode = pathNodeArray[endIndex];

        startNode.gCost = 0;
        startNode.hCost = CalculateDistanceCost(startNode, endNode);
        startNode.CalculateFCost();
        pathNodeArray[startIndex] = startNode;

        // Reset grid state
        NativeList<int> openList = new NativeList<int>(Allocator.Temp);
        NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

        openList.Add(startIndex);

        int safety = 0;

        while (openList.Length > 0) {
            int currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
            PathNode currentNode = pathNodeArray[currentNodeIndex];

            //Debug.Log("currentNode: " + currentNode);

            if (currentNodeIndex == endIndex) {
                // Reached final node
                break;
            }

            for (int i = 0; i < openList.Length; i++) {
                if (openList[i] == currentNodeIndex) {
                    openList.RemoveAtSwapBack(i);
                    break;
                }
            }

            safety++;
            if (safety >= 100) {
                Debug.Log("SAFETY TRIGGERED!");
                Debug.Log("currentNodeIndex: " + currentNodeIndex);
                for (int i = 0; i < openList.Length; i++) {
                    Debug.Log(openList[i]);
                }
                break;
            }
            
            closedList.Add(currentNodeIndex);

            NativeList<int2> neighbourList = new NativeList<int2>(Allocator.Temp);
            if (pathNodeNeighboursMultiHashMap.TryGetFirstValue(currentNodeIndex, out int2 neighbourPosition, out NativeMultiHashMapIterator<int> iterator)) {
                neighbourList.Add(neighbourPosition);
                while (pathNodeNeighboursMultiHashMap.TryGetNextValue(out neighbourPosition, ref iterator)) {
                    neighbourList.Add(neighbourPosition);
                }
            }

            for (int i = 0; i < neighbourList.Length; i++) {
                PathNode neighbourNode = pathNodeArray[CalculateIndex(neighbourList[i], gridSize.x)];
                if (closedList.Contains(neighbourNode.index)) continue; // Already searched

                int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighbourNode);
                if (tentativeGCost < neighbourNode.gCost) {
                    neighbourNode.cameFromNodeIndex = currentNodeIndex;
                    neighbourNode.gCost = tentativeGCost;
                    neighbourNode.hCost = CalculateDistanceCost(neighbourNode, endNode);
                    neighbourNode.CalculateFCost();
                    pathNodeArray[CalculateIndex(neighbourList[i], gridSize.x)] = neighbourNode;

                    //Debug.Log("Updated neighbour: " + neighbourNode);

                    if (!openList.Contains(neighbourNode.index)) {
                        openList.Add(neighbourNode.index);
                    }
                }
            }

            neighbourList.Dispose();
        }

        endNode = pathNodeArray[endIndex];
        NativeList<PathNode> path = CalculatePath(pathNodeArray, endNode);
        
        //Debug.Log("endNode: " + endNode + "; " + endNode.cameFromNodeIndex);

        if (path.Length == 0) {
            // Couldn't find path
            Debug.Log("Couldn't find path");
        } else {
            // Found path
            foreach (PathNode pathNode in path) {
                Debug.Log(pathNode);
            }
        }


        path.Dispose();
        closedList.Dispose();
        openList.Dispose();
        pathNodeArray.Dispose();
    }

    private int CalculateDistanceCost(PathNode a, PathNode b) {
        int xDistance = math.abs(a.x - b.x);
        int yDistance = math.abs(a.y - b.y);
        int remaining = math.abs(xDistance - yDistance);
        return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    }
    
    private NativeList<int2> GetNeighbours(int2 position, int2 gridSize) {
        NativeList<int2> neighbourList = new NativeList<int2>(Allocator.Temp);
        // Left
        if (position.x - 1 >= 0) neighbourList.Add(new int2(position.x - 1, position.y));
        // Right
        if (position.x + 1 < gridSize.x) neighbourList.Add(new int2(position.x + 1, position.y));
        // Up
        if (position.y + 1 < gridSize.y) neighbourList.Add(new int2(position.x, position.y + 1));
        // Down
        if (position.y - 1 >= 0) neighbourList.Add(new int2(position.x, position.y - 1));

        return neighbourList;
    }

    private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray) {
        PathNode lowestCostPathNode = pathNodeArray[openList[0]];
        for (int i = 1; i < openList.Length; i++) {
            PathNode testPathNode = pathNodeArray[openList[i]];
            if (testPathNode.fCost < lowestCostPathNode.fCost) {
                lowestCostPathNode = testPathNode;
            }
        }
        return lowestCostPathNode.index;
    }

    private NativeList<PathNode> CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode) {
        if (endNode.cameFromNodeIndex == -1) {
            // Couldn't find path
            return new NativeList<PathNode>(Allocator.Temp);
        } else {
            // Did find path
            NativeList<PathNode> path = new NativeList<PathNode>(Allocator.Temp);
            path.Add(endNode);

            PathNode currentNode = endNode;
            while (currentNode.cameFromNodeIndex != -1) {
                PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                path.Add(cameFromNode);
                currentNode = cameFromNode;
            }

            return path;
        }
    }
    
    public static int CalculateIndex(int2 position, int gridWidth) {
        return CalculateIndex(position.x, position.y, gridWidth);
    }

    public static int CalculateIndex(int x, int y, int gridWidth) {
        return x + y * gridWidth;
    }
    #endregion
}



[BurstCompile]
public struct FindPathJob : IJob {

    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    public int2 startPosition;
    public int2 endPosition;

    public NativeList<PathNode> path;

    public void Execute() {
        int2 gridSize = new int2(100, 100);
        
        NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.Temp);
        NativeMultiHashMap<int, int2> pathNodeNeighboursMultiHashMap = new NativeMultiHashMap<int, int2>(gridSize.x * gridSize.y, Allocator.Temp);

        for (int x = 0; x < gridSize.x; x++) {
            for (int y = 0; y < gridSize.y; y++) {
                int index = CalculateIndex(x, y, gridSize.x);
                PathNode pathNode = new PathNode { index = index, x = x, y = y };
                pathNode.gCost = int.MaxValue;
                pathNode.CalculateFCost();
                pathNode.cameFromNodeIndex = -1;
                pathNodeArray[index] = pathNode;

                NativeList<int2> neighbourList = GetNeighbours(new int2(x, y), gridSize);
                for (int i = 0; i < neighbourList.Length; i++) {
                    pathNodeNeighboursMultiHashMap.Add(index, neighbourList[i]);
                }
                neighbourList.Dispose();
                
            }
        }

        int startIndex = CalculateIndex(startPosition.x, startPosition.y, gridSize.x);
        int endIndex = CalculateIndex(endPosition.x, endPosition.y, gridSize.x);

        PathNode startNode = pathNodeArray[startIndex];
        PathNode endNode = pathNodeArray[endIndex];

        startNode.gCost = 0;
        startNode.hCost = CalculateDistanceCost(startNode, endNode);
        startNode.CalculateFCost();
        pathNodeArray[startIndex] = startNode;

        // Reset grid state
        NativeList<int> openList = new NativeList<int>(Allocator.Temp);
        NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

        openList.Add(startIndex);

        int safety = 0;

        while (openList.Length > 0) {
            int currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
            PathNode currentNode = pathNodeArray[currentNodeIndex];

            //Debug.Log("currentNode: " + currentNode);

            if (currentNodeIndex == endIndex) {
                // Reached final node
                break;
            }

            for (int i = 0; i < openList.Length; i++) {
                if (openList[i] == currentNodeIndex) {
                    openList.RemoveAtSwapBack(i);
                    break;
                }
            }

            /*
            safety++;
            if (safety >= 10000) {
                Debug.Log("SAFETY TRIGGERED!");
                Debug.Log("currentNode: " + currentNode);
                for (int i = 0; i < openList.Length; i++) {
                    Debug.Log(pathNodeArray[openList[i]]);
                }
                break;
            }
            //*
            
            closedList.Add(currentNodeIndex);

            NativeList<int2> neighbourList = new NativeList<int2>(Allocator.Temp);
            if (pathNodeNeighboursMultiHashMap.TryGetFirstValue(currentNodeIndex, out int2 neighbourPosition, out NativeMultiHashMapIterator<int> iterator)) {
                neighbourList.Add(neighbourPosition);
                while (pathNodeNeighboursMultiHashMap.TryGetNextValue(out neighbourPosition, ref iterator)) {
                    neighbourList.Add(neighbourPosition);
                }
            }

            for (int i = 0; i < neighbourList.Length; i++) {
                PathNode neighbourNode = pathNodeArray[CalculateIndex(neighbourList[i], gridSize.x)];
                if (closedList.Contains(neighbourNode.index)) continue; // Already searched

                int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighbourNode);
                if (tentativeGCost < neighbourNode.gCost) {
                    neighbourNode.cameFromNodeIndex = currentNodeIndex;
                    neighbourNode.gCost = tentativeGCost;
                    neighbourNode.hCost = CalculateDistanceCost(neighbourNode, endNode);
                    neighbourNode.CalculateFCost();
                    pathNodeArray[CalculateIndex(neighbourList[i], gridSize.x)] = neighbourNode;

                    //Debug.Log("Updated neighbour: " + neighbourNode);

                    if (!openList.Contains(neighbourNode.index)) {
                        openList.Add(neighbourNode.index);
                    }
                }
            }

            neighbourList.Dispose();
        }

        endNode = pathNodeArray[endIndex];
        CalculatePath(pathNodeArray, endNode, path);
        
        //Debug.Log("endNode: " + endNode + "; " + endNode.cameFromNodeIndex);

        /*
        if (path.Length == 0) {
            // Couldn't find path
            Debug.Log("Couldn't find path");
        } else {
            // Found path
            foreach (PathNode pathNode in path) {
                Debug.Log(pathNode);
            }
        }
        *


        //path.Dispose();
        closedList.Dispose();
        openList.Dispose();
        pathNodeArray.Dispose();
    }

    private int CalculateDistanceCost(PathNode a, PathNode b) {
        int xDistance = math.abs(a.x - b.x);
        int yDistance = math.abs(a.y - b.y);
        int remaining = math.abs(xDistance - yDistance);
        return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
        //return MOVE_STRAIGHT_COST * xDistance + MOVE_STRAIGHT_COST * yDistance;
    }
    
    private NativeList<int2> GetNeighbours(int2 position, int2 gridSize) {
        NativeList<int2> neighbourList = new NativeList<int2>(Allocator.Temp);
        // Left
        if (position.x - 1 >= 0) neighbourList.Add(new int2(position.x - 1, position.y));
        // Right
        if (position.x + 1 < gridSize.x) neighbourList.Add(new int2(position.x + 1, position.y));
        // Up
        if (position.y + 1 < gridSize.y) neighbourList.Add(new int2(position.x, position.y + 1));
        // Down
        if (position.y - 1 >= 0) neighbourList.Add(new int2(position.x, position.y - 1));
        
        // Left Down
        if ((position.x - 1 >= 0) && (position.y - 1 >= 0)) neighbourList.Add(new int2(position.x - 1, position.y - 1));
        // Left Up
        if ((position.x - 1 >= 0) && (position.y + 1 < gridSize.y)) neighbourList.Add(new int2(position.x - 1, position.y + 1));
        
        // Right Down
        if ((position.x + 1 < gridSize.x) && (position.y - 1 >= 0)) neighbourList.Add(new int2(position.x + 1, position.y - 1));
        // Right Up
        if ((position.x + 1 < gridSize.x) && (position.y + 1 < gridSize.y)) neighbourList.Add(new int2(position.x + 1, position.y + 1));

        return neighbourList;
    }

    private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray) {
        PathNode lowestCostPathNode = pathNodeArray[openList[0]];
        for (int i = 1; i < openList.Length; i++) {
            PathNode testPathNode = pathNodeArray[openList[i]];
            if (testPathNode.fCost < lowestCostPathNode.fCost) {
                lowestCostPathNode = testPathNode;
            }
        }
        return lowestCostPathNode.index;
    }

    private NativeList<PathNode> CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode) {
        if (endNode.cameFromNodeIndex == -1) {
            // Couldn't find path
            return new NativeList<PathNode>(Allocator.Temp);
        } else {
            // Did find path
            NativeList<PathNode> path = new NativeList<PathNode>(Allocator.Temp);
            path.Add(endNode);

            PathNode currentNode = endNode;
            while (currentNode.cameFromNodeIndex != -1) {
                PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                path.Add(cameFromNode);
                currentNode = cameFromNode;
            }

            return path;
        }
    }

    private void CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode, NativeList<PathNode> path) {
        if (endNode.cameFromNodeIndex == -1) {
            // Couldn't find path
        } else {
            // Did find path
            path.Add(endNode);

            PathNode currentNode = endNode;
            while (currentNode.cameFromNodeIndex != -1) {
                PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                path.Add(cameFromNode);
                currentNode = cameFromNode;
            }
        }
    }
    
    public static int CalculateIndex(int2 position, int gridWidth) {
        return CalculateIndex(position.x, position.y, gridWidth);
    }

    public static int CalculateIndex(int x, int y, int gridWidth) {
        return x + y * gridWidth;
    }

}


public struct PathNode {

    public int index;
    public int x;
    public int y;

    public int gCost;
    public int hCost;
    public int fCost;

    public int cameFromNodeIndex;

    public void CalculateFCost() {
        fCost = gCost + hCost;
    }

    public override string ToString() {
        return x + ", " + y + "; " + "index:" + index + "; fCost:" + fCost + "; gCost:" + gCost + "; hCost:" + hCost;
    }

}
*/
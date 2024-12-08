using Debugging;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public struct Pathfinding : IComponentData
{
    public int2 StartPosition;
    public int2 EndPosition;
}

[UpdateInGroup(typeof(UnitStateSystemGroup))]
[UpdateAfter(typeof(PathFollowSystem))]
public partial struct PathfindingSystem : ISystem
{
    private const int MoveStraightCost = 10;
    private const int MoveDiagonalCost = 14;
    private const int MaxPathfindingSchedulesPerFrame = 200;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridManager>();
        state.RequireForUpdate<DebugToggleManager>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var isDebugging = SystemAPI.GetSingleton<DebugToggleManager>().DebugPathfinding;
        var gridManager = SystemAPI.GetSingleton<GridManager>();
        var walkableGrid = gridManager.WalkableGrid;
        var gridWidth = gridManager.Width;
        var gridHeight = gridManager.Height;
        var gridSize = new int2(gridWidth, gridHeight);

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var currentAmountOfSchedules = 0;
        var startCells = new NativeList<int2>(MaxPathfindingSchedulesPerFrame, Allocator.TempJob);
        var endCells = new NativeList<int2>(MaxPathfindingSchedulesPerFrame, Allocator.TempJob);
        var entities = new NativeList<Entity>(MaxPathfindingSchedulesPerFrame, Allocator.TempJob);

        foreach (var (pathfindingParams, pathPosition, pathFollow, localTransform, entity) in SystemAPI
                     .Query<RefRO<Pathfinding>, DynamicBuffer<PathPosition>, RefRW<PathFollow>, RefRO<LocalTransform>>()
                     .WithEntityAccess())
        {
            if (currentAmountOfSchedules > MaxPathfindingSchedulesPerFrame)
            {
                continue;
            }

            currentAmountOfSchedules++;

            var startCell = pathfindingParams.ValueRO.StartPosition;
            var endCell = pathfindingParams.ValueRO.EndPosition;

            if (startCell.x == endCell.x && startCell.y == endCell.y)
            {
                // No need for pathfinding. We'll just set our current cell as the path-target.
                pathPosition.Clear();
                pathPosition.Add(new PathPosition
                {
                    Position = endCell
                });
                pathFollow.ValueRW.PathIndex = 0;
            }
            else
            {
                startCells.Add(startCell);
                endCells.Add(endCell);
                entities.Add(entity);
                gridManager.TryClearOccupant(startCell, entity);
            }

            ecb.RemoveComponent<Pathfinding>(entity);
        }

        var findPathJobParallel = new FindPathJobParallel
        {
            WalkableGrid = walkableGrid,
            GridSize = gridSize,
            StartPositions = startCells.AsArray(),
            EndPositions = endCells.AsArray(),
            Entities = entities.AsArray(),
            PathFollowLookup = SystemAPI.GetComponentLookup<PathFollow>(),
            PathPositionLookup = SystemAPI.GetBufferLookup<PathPosition>(),
            EcbParallelWriter = ecb.AsParallelWriter(),
            IsDebugging = isDebugging
        };

        SystemAPI.SetSingleton(gridManager);

        state.Dependency = findPathJobParallel.Schedule(entities.Length, 1);
        startCells.Dispose(state.Dependency);
        endCells.Dispose(state.Dependency);
        entities.Dispose(state.Dependency);
    }

    private static NativeArray<PathNode> GetPathNodeArray(int2 gridSize, NativeArray<WalkableCell> grid)
    {
        var pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.Temp);
        PopulatePathNodeArray(ref pathNodeArray, grid, gridSize);

        return pathNodeArray;
    }

    private static void PopulatePathNodeArray(ref NativeArray<PathNode> pathNodeArray, NativeArray<WalkableCell> grid,
        int2 gridSize)
    {
        for (var x = 0; x < gridSize.x; x++)
        {
            for (var y = 0; y < gridSize.y; y++)
            {
                var pathNode = new PathNode
                {
                    x = x,
                    y = y,
                    index = GridHelpers.GetIndex(gridSize.y, x, y),
                    gCost = int.MaxValue
                };
                pathNode.isWalkable = grid[pathNode.index].IsWalkable;
                pathNode.cameFromNodeIndex = -1;

                pathNodeArray[pathNode.index] = pathNode;
            }
        }
    }

    private static void CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode,
        DynamicBuffer<PathPosition> pathPositionBuffer)
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

    private static bool IsPositionInsideGrid(int x, int y, int2 gridSize)
    {
        return
            x >= 0 &&
            y >= 0 &&
            x < gridSize.x &&
            y < gridSize.y;
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
    private struct FindPathJobParallel : IJobParallelFor
    {
        [ReadOnly] public NativeArray<WalkableCell> WalkableGrid;
        [ReadOnly] public int2 GridSize;

        [ReadOnly] public NativeArray<int2> StartPositions;
        [ReadOnly] public NativeArray<int2> EndPositions;
        [ReadOnly] public NativeArray<Entity> Entities;

        [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<PathFollow> PathFollowLookup;

        [NativeDisableContainerSafetyRestriction]
        public BufferLookup<PathPosition> PathPositionLookup;

        public EntityCommandBuffer.ParallelWriter EcbParallelWriter;
        [ReadOnly] public bool IsDebugging;

        public void Execute(int index)
        {
            var entity = Entities[index];
            var startPosition = StartPositions[index];
            var endPosition = EndPositions[index];

            var pathNodeArray = GetPathNodeArray(GridSize, WalkableGrid);

            FindPath(pathNodeArray, startPosition, endPosition);

            var pathPosition = PathPositionLookup[entity];
            pathPosition.Clear();

            var endNodeIndex = GridHelpers.GetIndex(GridSize.y, endPosition.x, endPosition.y);
            var endNode = pathNodeArray[endNodeIndex];

            if (endNode.cameFromNodeIndex == -1)
            {
                // Didn't find a path!
                if (IsDebugging)
                {
                    DebugHelper.LogError("Didn't find a path!");
                    EcbParallelWriter.AddComponent(index, EcbParallelWriter.CreateEntity(index),
                        new DebugPopupEvent
                        {
                            Type = DebugPopupEventType.PathNotFoundStart,
                            Cell = startPosition
                        });
                    EcbParallelWriter.AddComponent(index, EcbParallelWriter.CreateEntity(index),
                        new DebugPopupEvent
                        {
                            Type = DebugPopupEventType.PathNotFoundEnd,
                            Cell = endPosition
                        });
                }

                PathFollowLookup[entity] = new PathFollow(-1);
            }
            else
            {
                // Found a path
                CalculatePath(pathNodeArray, endNode, pathPosition);
                PathFollowLookup[entity] = new PathFollow(pathPosition.Length - 1);
            }
        }

        private void FindPath(NativeArray<PathNode> pathNodeArray, int2 startPosition, int2 endPosition)
        {
            for (var i = 0; i < pathNodeArray.Length; i++)
            {
                var pathNode = pathNodeArray[i];
                pathNode.hCost = CalculateDistanceCost(pathNode.x, pathNode.y, endPosition.x, endPosition.y);
                pathNode.cameFromNodeIndex = -1;

                pathNodeArray[i] = pathNode;
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

            var endNodeIndex = GridHelpers.GetIndex(GridSize.y, endPosition.x, endPosition.y);
            var startNodeIndex = GridHelpers.GetIndex(GridSize.y, startPosition.x, startPosition.y);

            var startNode = pathNodeArray[startNodeIndex];
            startNode.gCost = 0;
            startNode.CalculateFCost();
            pathNodeArray[startNode.index] = startNode;

            var openList = new NativeList<int>(Allocator.Temp);
            var closedHashSet = new NativeHashSet<int>(1, Allocator.Temp);

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

                closedHashSet.Add(currentNodeIndex);

                for (var i = 0; i < neighbourOffsetArrayX.Length; i++)
                {
                    var neighbourPosX = currentNode.x + neighbourOffsetArrayX[i];
                    var neighbourPosY = currentNode.y + neighbourOffsetArrayY[i];

                    if (!IsPositionInsideGrid(neighbourPosX, neighbourPosY, GridSize))
                    {
                        // Neighbour not valid position
                        continue;
                    }

                    var neighbourNodeIndex = GridHelpers.GetIndex(GridSize.y, neighbourPosX, neighbourPosY);

                    if (closedHashSet.Contains(neighbourNodeIndex))
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

                    var tentativeGCost = currentNode.gCost +
                                         CalculateDistanceCost(currentNode.x, currentNode.y, neighbourPosX,
                                             neighbourPosY);
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

            neighbourOffsetArrayX.Dispose();
            neighbourOffsetArrayY.Dispose();
            openList.Dispose();
            closedHashSet.Dispose();
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
}
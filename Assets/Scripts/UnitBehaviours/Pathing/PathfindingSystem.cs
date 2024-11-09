using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateInGroup(typeof(UnitStateSystemGroup))]
[UpdateAfter(typeof(PathFollowSystem))]
public partial struct PathfindingSystem : ISystem
{
    private const int MoveStraightCost = 10;
    private const int MoveDiagonalCost = 14;
    private const int MaxPathfindingSchedulesPerFrame = 200;
    private SystemHandle _gridManagerSystemHandle;

    public void OnCreate(ref SystemState state)
    {
        _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
        var walkableGrid = gridManager.WalkableGrid;
        var gridWidth = gridManager.Width;
        var gridHeight = gridManager.Height;
        var gridSize = new int2(gridWidth, gridHeight);

        var findPathJobList = new NativeList<FindPathJob>(Allocator.Temp);
        var jobHandleList = new NativeList<JobHandle>(Allocator.Temp);
        var entityCommandBuffer = new EntityCommandBuffer(state.WorldUpdateAllocator);

        var currentAmountOfSchedules = 0;

        foreach (var (pathfindingParams, pathPosition, pathFollow, entity) in SystemAPI
                     .Query<RefRO<PathfindingParams>, DynamicBuffer<PathPosition>, RefRW<PathFollow>>()
                     .WithEntityAccess())
        {
            if (currentAmountOfSchedules > MaxPathfindingSchedulesPerFrame)
            {
                continue;
            }

            currentAmountOfSchedules++;

            var startPosition = pathfindingParams.ValueRO.StartPosition;
            var endPosition = pathfindingParams.ValueRO.EndPosition;
            var findPathJob = new FindPathJob
            {
                WalkableGrid = walkableGrid,
                GridSize = gridSize,
                StartPosition = startPosition,
                EndPosition = endPosition,
                Entity = entity,
                PathFollowLookup = SystemAPI.GetComponentLookup<PathFollow>(),
                PathPositionLookup = SystemAPI.GetBufferLookup<PathPosition>()
            };
            findPathJobList.Add(findPathJob);
            jobHandleList.Add(findPathJob.Schedule());

            gridManager.TryClearOccupant(startPosition, entity);
            entityCommandBuffer.RemoveComponent<PathfindingParams>(entity);
        }

        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);

        JobHandle.CompleteAll(jobHandleList.AsArray());

        entityCommandBuffer.Playback(state.EntityManager);
        findPathJobList.Dispose();
        jobHandleList.Dispose();
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
    private struct FindPathJob : IJob
    {
        [ReadOnly] public NativeArray<WalkableCell> WalkableGrid;

        public int2 GridSize;

        public int2 StartPosition;
        public int2 EndPosition;

        public Entity Entity;

        [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<PathFollow> PathFollowLookup;

        [NativeDisableContainerSafetyRestriction]
        public BufferLookup<PathPosition> PathPositionLookup;

        public void Execute()
        {
            var pathNodeArray = GetPathNodeArray(GridSize, WalkableGrid);

            FindPath(pathNodeArray);

            var pathPosition = PathPositionLookup[Entity];
            pathPosition.Clear();

            var endNodeIndex = GridHelpers.GetIndex(GridSize.y, EndPosition.x, EndPosition.y);
            var endNode = pathNodeArray[endNodeIndex];

            if (endNode.cameFromNodeIndex == -1)
            {
                // Didn't find a path!
                // Debug.LogError("Didn't find a path!");
                PathFollowLookup[Entity] = new PathFollow
                {
                    PathIndex = -1
                };
            }
            else
            {
                // Found a path
                CalculatePath(pathNodeArray, endNode, pathPosition);
                PathFollowLookup[Entity] = new PathFollow
                {
                    PathIndex = pathPosition.Length - 1
                };
            }
        }

        private void FindPath(NativeArray<PathNode> pathNodeArray)
        {
            for (var i = 0; i < pathNodeArray.Length; i++)
            {
                var pathNode = pathNodeArray[i];
                pathNode.hCost = CalculateDistanceCost(pathNode.x, pathNode.y, EndPosition.x, EndPosition.y);
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

            var endNodeIndex = GridHelpers.GetIndex(GridSize.y, EndPosition.x, EndPosition.y);
            var startNodeIndex = GridHelpers.GetIndex(GridSize.y, StartPosition.x, StartPosition.y);

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

    public void SetIsWalkable(bool isWalkable)
    {
        this.isWalkable = isWalkable;
    }
}
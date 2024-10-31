using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateAfter(typeof(GridManagerSystem))]
public partial class Pathfinding : SystemBase
{
    private const int MoveStraightCost = 10;
    private const int MoveDiagonalCost = 14;
    private static NativeArray<PathNode> PathNodeArrayTemplate;
    private SystemHandle _gridManagerSystemHandle;

    protected override void OnCreate()
    {
        _gridManagerSystemHandle = World.GetExistingSystem<GridManagerSystem>();
    }

    protected override void OnUpdate()
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
        var walkableGrid = gridManager.WalkableGrid;
        var gridWidth = gridManager.Width;
        var gridHeight = gridManager.Height;
        var gridSize = new int2(gridWidth, gridHeight);
        if (!PathNodeArrayTemplate.IsCreated)
        {
            PathNodeArrayTemplate = new NativeArray<PathNode>(gridWidth * gridHeight, Allocator.Persistent);
        }

        var findPathJobList = new NativeList<FindPathJob>(Allocator.Temp);
        var jobHandleList = new NativeList<JobHandle>(Allocator.Temp);
        var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

        var maxPathfindingSchedulesPerFrame = Globals.MaxPathfindingPerFrame();
        var currentAmountOfSchedules = 0;

        var templateIsCreated = false;

        foreach (var (pathfindingParams, pathPositionBuffer, entity) in SystemAPI.Query<RefRO<PathfindingParams>, DynamicBuffer<PathPosition>>()
                     .WithEntityAccess())
        {
            if (!templateIsCreated)
            {
                templateIsCreated = true;
                UpdatePathNodeArrayTemplate(walkableGrid, gridSize);
            }

            if (currentAmountOfSchedules > maxPathfindingSchedulesPerFrame)
            {
                continue;
            }

            currentAmountOfSchedules++;

            var startPosition = pathfindingParams.ValueRO.StartPosition;
            var endPosition = pathfindingParams.ValueRO.EndPosition;
            var findPathJob = new FindPathJob
            {
                GridSize = gridSize,
                PathNodeArray = GetPathNodeArray(gridSize),
                StartPosition = startPosition,
                EndPosition = endPosition,
                Entity = entity,
                PathFollowLookup = GetComponentLookup<PathFollow>()
            };
            findPathJobList.Add(findPathJob);
            jobHandleList.Add(findPathJob.Schedule());

            gridManager.TryClearOccupant(startPosition, entity);
            gridManager.TryClearInteractor(startPosition, entity);
            entityCommandBuffer.RemoveComponent<PathfindingParams>(entity);
        }

        SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);

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

        entityCommandBuffer.Playback(EntityManager);
        findPathJobList.Dispose();
        jobHandleList.Dispose();
    }

    protected override void OnDestroy()
    {
        PathNodeArrayTemplate.Dispose();
    }

    private NativeArray<PathNode> GetPathNodeArray(int2 gridSize)
    {
        var pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.TempJob);
        pathNodeArray.CopyFrom(PathNodeArrayTemplate);

        return pathNodeArray;
    }

    private void UpdatePathNodeArrayTemplate(NativeArray<WalkableCell> grid, int2 gridSize)
    {
        for (var x = 0; x < gridSize.x; x++)
        {
            for (var y = 0; y < gridSize.y; y++)
            {
                var pathNode = new PathNode();
                pathNode.x = x;
                pathNode.y = y;
                pathNode.index = GridHelpers.GetIndex(gridSize.y, x, y);
                pathNode.gCost = int.MaxValue;
                pathNode.isWalkable = grid[pathNode.index].IsWalkable;
                pathNode.cameFromNodeIndex = -1;

                PathNodeArrayTemplate[pathNode.index] = pathNode;
            }
        }
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

            var endNodeIndex = GridHelpers.GetIndex(GridSize.y, pathFindingParams.EndPosition.x, pathFindingParams.EndPosition.y);
            var endNode = PathNodeArray[endNodeIndex];

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

            var startNode = PathNodeArray[startNodeIndex];
            startNode.gCost = 0;
            startNode.CalculateFCost();
            PathNodeArray[startNode.index] = startNode;

            var openList = new NativeList<int>(Allocator.Temp);
            var closedHashSet = new NativeHashSet<int>(1, Allocator.Temp);

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
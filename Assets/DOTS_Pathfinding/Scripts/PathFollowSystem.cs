using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial class PathFollowSystem : SystemBase
{
    private Random random;

    protected override void OnCreate()
    {
        random = new Random(56);
    }

    // protected override JobHandle OnUpdate(JobHandle inputDeps) {
    //     float deltaTime = Time.DeltaTime;
    //
    //     return Entities.ForEach((Entity entity, DynamicBuffer<PathPosition> pathPositionBuffer, ref Translation translation, ref PathFollow pathFollow) => {
    //         if (pathFollow.pathIndex >= 0) {
    //             // Has path to follow
    //             PathPosition pathPosition = pathPositionBuffer[pathFollow.pathIndex];
    //
    //             float3 targetPosition = new float3(pathPosition.position.x, pathPosition.position.y, 0);
    //             float3 moveDir = math.normalizesafe(targetPosition - translation.Value);
    //             float moveSpeed = 3f;
    //
    //             translation.Value += moveDir * moveSpeed * deltaTime;
    //             
    //             if (math.distance(translation.Value, targetPosition) < .1f) {
    //                 // Next waypoint
    //                 pathFollow.pathIndex--;
    //             }
    //         }
    //     }).Schedule(inputDeps);
    // }

    private void ValidateGridPosition(ref int x, ref int y)
    {
        x = math.clamp(x, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetWidth() - 1);
        y = math.clamp(y, 0, PathfindingGridSetup.Instance.pathfindingGrid.GetHeight() - 1);
    }

    protected override void OnUpdate()
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (translation, pathFollow, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<PathFollow>>().WithEntityAccess())
        {
            var pathPositionBuffer = SystemAPI.GetBuffer<PathPosition>(entity);

            if (pathFollow.ValueRO.pathIndex >= 0)
            {
                // Has path to follow
                var pathPosition = pathPositionBuffer[pathFollow.ValueRO.pathIndex];

                var targetPosition = new float3(pathPosition.position.x, pathPosition.position.y, 0);
                var moveDir = math.normalizesafe(targetPosition - translation.ValueRO.Position);
                var moveSpeed = 3f;

                translation.ValueRW.Position += moveDir * moveSpeed * deltaTime;

                if (math.distance(translation.ValueRO.Position, targetPosition) < .1f)
                {
                    // Next waypoint
                    pathFollow.ValueRW.pathIndex--;
                }
            }
        }
    }
}

[UpdateAfter(typeof(PathFollowSystem))]
[DisableAutoCreation]
public partial class PathFollowGetNewPathSystem : SystemBase
{
    private Random random;

    // private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        random = new Random(56);
        // endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var endSimulationEcbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var entityCommandBuffer = endSimulationEcbSystem.CreateCommandBuffer(World.DefaultGameObjectInjectionWorld.Unmanaged);

        var mapWidth = PathfindingGridSetup.Instance.pathfindingGrid.GetWidth();
        var mapHeight = PathfindingGridSetup.Instance.pathfindingGrid.GetHeight();
        var originPosition = float3.zero;
        var cellSize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();
        var random = new Random(this.random.NextUInt(1, 10000));

        // EntityCommandBuffer.Concurrent entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();


        JobHandle jobHandle =   foreach (var () in SystemAPI.Query<RefRW<PathFollow>, RefRW<LocalTransform>, EntityIndexInQuery>()) =>
        {
            if (pathFollow.pathIndex == -1)
            {
                GetXY(translation.Value + new float3(1, 1, 0) * cellSize * +.5f, originPosition, cellSize, out var startX, out var startY);

                ValidateGridPosition(ref startX, ref startY, mapWidth, mapHeight);

                var endX = random.NextInt(0, mapWidth);
                var endY = random.NextInt(0, mapHeight);

                entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new PathfindingParams
                {
                    startPosition = new int2(startX, startY), endPosition = new int2(endX, endY)
                });
            }
        }).Schedule(inputDeps);

        // JobHandle jobHandle = Entities.WithNone<PathfindingParams>().ForEach(
        //     (Entity entity, int entityInQueryIndex, in PathFollow pathFollow, in Translation translation) =>
        //     {
        //         if (pathFollow.pathIndex == -1)
        //         {
        //             GetXY(translation.Value + new float3(1, 1, 0) * cellSize * +.5f, originPosition, cellSize, out var startX, out var startY);
        //
        //             ValidateGridPosition(ref startX, ref startY, mapWidth, mapHeight);
        //
        //             var endX = random.NextInt(0, mapWidth);
        //             var endY = random.NextInt(0, mapHeight);
        //
        //             entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new PathfindingParams
        //             {
        //                 startPosition = new int2(startX, startY), endPosition = new int2(endX, endY)
        //             });
        //         }
        //     }).Schedule(inputDeps);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);
    }
    // protected override JobHandle OnUpdate(JobHandle inputDeps)
    // {
    //     var mapWidth = PathfindingGridSetup.Instance.pathfindingGrid.GetWidth();
    //     var mapHeight = PathfindingGridSetup.Instance.pathfindingGrid.GetHeight();
    //     var originPosition = float3.zero;
    //     var cellSize = PathfindingGridSetup.Instance.pathfindingGrid.GetCellSize();
    //     var random = new Random(this.random.NextUInt(1, 10000));
    //
    //     EntityCommandBuffer.Concurrent entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
    //
    //     JobHandle jobHandle = Entities.WithNone<PathfindingParams>().ForEach(
    //         (Entity entity, int entityInQueryIndex, in PathFollow pathFollow, in Translation translation) =>
    //         {
    //             if (pathFollow.pathIndex == -1)
    //             {
    //                 GetXY(translation.Value + new float3(1, 1, 0) * cellSize * +.5f, originPosition, cellSize, out var startX, out var startY);
    //
    //                 ValidateGridPosition(ref startX, ref startY, mapWidth, mapHeight);
    //
    //                 var endX = random.NextInt(0, mapWidth);
    //                 var endY = random.NextInt(0, mapHeight);
    //
    //                 entityCommandBuffer.AddComponent(entityInQueryIndex, entity, new PathfindingParams
    //                 {
    //                     startPosition = new int2(startX, startY), endPosition = new int2(endX, endY)
    //                 });
    //             }
    //         }).Schedule(inputDeps);
    //
    //     endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);
    //
    //     return jobHandle;
    // }

    private static void ValidateGridPosition(ref int x, ref int y, int width, int height)
    {
        x = math.clamp(x, 0, width - 1);
        y = math.clamp(y, 0, height - 1);
    }

    private static void GetXY(float3 worldPosition, float3 originPosition, float cellSize, out int x, out int y)
    {
        x = (int)math.floor((worldPosition - originPosition).x / cellSize);
        y = (int)math.floor((worldPosition - originPosition).y / cellSize);
    }
}
using Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct SpriteSheetSortingManager : IComponentData
{
    public NativeArray<Matrix4x4> SpriteMatrixArray;
    public NativeArray<Vector4> SpriteUvArray;
}

[UpdateInGroup(typeof(PreRenderingSystemGroup), OrderLast = true)]
[BurstCompile]
public partial struct SpriteSheetQuickSortSystem : ISystem
{
    private EntityQuery _entityQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CameraInformation>();
        var singletonEntity = state.EntityManager.CreateSingleton<SpriteSheetSortingManager>();
        SystemAPI.SetComponent(singletonEntity, new SpriteSheetSortingManager
        {
            SpriteMatrixArray = new NativeArray<Matrix4x4>(1, Allocator.TempJob),
            SpriteUvArray = new NativeArray<Vector4>(1, Allocator.TempJob)
        });
        _entityQuery = state.GetEntityQuery(ComponentType.ReadOnly<SpriteSheetAnimation>(),
            ComponentType.ReadOnly<LocalToWorld>());

        state.RequireForUpdate<BeginPresentationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<SpriteSheetSortingManager>();
    }

    public void OnDestroy(ref SystemState state)
    {
        var singleton = SystemAPI.GetSingletonRW<SpriteSheetSortingManager>();
        singleton.ValueRW.SpriteMatrixArray.Dispose();
        singleton.ValueRW.SpriteUvArray.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency.Complete();

        var cameraInformation = SystemAPI.GetSingleton<CameraInformation>();

        var cameraPosition = cameraInformation.CameraPosition;
        var screenRatio = cameraInformation.ScreenRatio;
        var orthographicSize = cameraInformation.OrthographicSize;

        var cullBuffer = 1f; // We add some buffer, so culling is not noticable
        var cameraSizeX = orthographicSize * screenRatio + cullBuffer;
        var cameraSizeY = orthographicSize + cullBuffer;

        var xLeft = cameraPosition.x - cameraSizeX;
        var xRight = cameraPosition.x + cameraSizeX;
        var yTop = cameraPosition.y + cameraSizeY;
        var yBottom = cameraPosition.y - cameraSizeY;

        var splitTimes = 4;
        var jobCount = (int)math.pow(2, splitTimes) - 1;
        var quickPivots = new NativeArray<float>(jobCount, Allocator.Temp);

        var normalizedPivotInterval = 1f;
        var normalizedPivot = 0.5f;
        var normalizedPivotSum = normalizedPivot;
        for (var i = 0; i < jobCount; i++)
        {
            quickPivots[i] = yBottom + normalizedPivotSum * (yTop - yBottom);
            normalizedPivotSum -= normalizedPivotInterval;
            if (normalizedPivotSum <= 0)
            {
                normalizedPivot *= 0.5f;
                normalizedPivotInterval *= 0.5f;
                normalizedPivotSum = 1 - normalizedPivot;
            }
        }

        var jobBatches = new NativeArray<NativeArray<JobHandle>>(splitTimes, Allocator.Temp);

        var finishedJobBatches = 0;
        var jobBatchSize = 1;
        var jobBatchIndex = 0;

        for (var i = 0; i < quickPivots.Length; i++)
        {
            if (i == finishedJobBatches + jobBatchSize - 1)
            {
                // jobBatches follow the pattern of 1, 2, 4, 8, etc...
                jobBatches[jobBatchIndex] = new NativeArray<JobHandle>(jobBatchSize, Allocator.Temp);
                finishedJobBatches += jobBatchSize;
                jobBatchSize *= 2;
                jobBatchIndex++;
            }
        }

        // Select sprite sheets that need sorting:
        var spriteSheetsToSort = new NativeQueue<RenderData>(Allocator.TempJob);
        new CullJob
        {
            XLeft = xLeft,
            XRight = xRight,
            YTop = yTop,
            YBottom = yBottom,
            NativeQueue = spriteSheetsToSort
        }.Run(_entityQuery);

        var outQueues = new NativeArray<NativeQueue<RenderData>>(1 + quickPivots.Length * 2, Allocator.Temp);
        outQueues[0] = spriteSheetsToSort;

        var jobList = new NativeList<JobHandle>(Allocator.Temp);
        var jobIndex = 0;
        var jobBatchesLength = jobBatches.Length;
        for (var i = 0; i < jobBatchesLength; i++)
        {
            var batchSize = jobBatches[i].Length;
            for (var j = 0; j < batchSize; j++)
            {
                var pivot = quickPivots[jobIndex];
                var outQueueIndex1 = jobIndex + batchSize + j;
                var outQueueIndex2 = outQueueIndex1 + 1;
                var quickSortJob = new QuickSortJob
                {
                    Pivot = pivot,
                    InQueue = outQueues[jobIndex],
                    OutQueue1 = outQueues[outQueueIndex1] = new NativeQueue<RenderData>(Allocator.TempJob),
                    OutQueue2 = outQueues[outQueueIndex2] = new NativeQueue<RenderData>(Allocator.TempJob)
                };
                jobList.Add(quickSortJob.Schedule());
                jobIndex++;
            }

            JobHandle.CompleteAll(jobList.AsArray());
            jobList.Clear();
        }

        jobList.Dispose();

        for (var i = 0; i < jobBatches.Length; i++)
        {
            jobBatches[i].Dispose();
        }

        jobBatches.Dispose();

        var outQueuesLength = outQueues.Length;
        var jobs = new NativeArray<JobHandle>(outQueuesLength, Allocator.Temp);
        var outArrays = new NativeArray<NativeArray<RenderData>>(outQueuesLength, Allocator.Temp);
        for (var i = 0; i < outQueuesLength; i++)
        {
            var nativeQueueToArrayJob = new NativeQueueToArrayJob
            {
                NativeQueue = outQueues[i],
                NativeArray = outArrays[i] = new NativeArray<RenderData>(outQueues[i].Count, Allocator.TempJob)
            };
            jobs[i] = nativeQueueToArrayJob.Schedule();
        }

        JobHandle.CompleteAll(jobs);

        var visibleEntityTotal = 0;
        for (var index = 0; index < outArrays.Length; index++)
        {
            visibleEntityTotal += outArrays[index].Length;
        }

        // Clear data
        var singleton = SystemAPI.GetSingletonRW<SpriteSheetSortingManager>();
        singleton.ValueRW.SpriteMatrixArray.Dispose();
        singleton.ValueRW.SpriteUvArray.Dispose();
        singleton.ValueRW.SpriteMatrixArray = new NativeArray<Matrix4x4>(visibleEntityTotal, Allocator.TempJob);
        singleton.ValueRW.SpriteUvArray = new NativeArray<Vector4>(visibleEntityTotal, Allocator.TempJob);
        var spriteMatrixArray = singleton.ValueRO.SpriteMatrixArray;
        var spriteUvArray = singleton.ValueRO.SpriteUvArray;

        var sharedNativeArray = new NativeArray<RenderData>(visibleEntityTotal, Allocator.TempJob);
        var startIndexes = new NativeArray<int>(outArrays.Length, Allocator.TempJob);
        var endIndexes = new NativeArray<int>(outArrays.Length, Allocator.TempJob);
        var sharedIndex = 0;

        for (var i = 0; i < outArrays.Length; i++)
        {
            startIndexes[i] = i > 0 ? endIndexes[i - 1] : 0;
            endIndexes[i] = startIndexes[i] + outArrays[i].Length;
            for (var j = 0; j < outArrays[i].Length; j++)
            {
                sharedNativeArray[sharedIndex] = outArrays[i][j];
                sharedIndex++;
            }
        }

        for (var i = 0; i < outArrays.Length; i++)
        {
            var sortByPositionParallelJob = new SortByPositionParallelJob
            {
                SharedNativeArray = sharedNativeArray,
                StartIndex = startIndexes[i],
                EndIndex = endIndexes[i]
            };
            jobs[i] = sortByPositionParallelJob.Schedule();
        }

        var dependency = JobHandle.CombineDependencies(jobs);

        var fillArraysJob = new FillArraysJob
        {
            SortedArray = sharedNativeArray,
            MatrixArray = spriteMatrixArray,
            UvArray = spriteUvArray
        };
        dependency = fillArraysJob.Schedule(visibleEntityTotal, 10, dependency);

        sharedNativeArray.Dispose(dependency);
        startIndexes.Dispose(dependency);
        endIndexes.Dispose(dependency);

        state.Dependency = dependency;

        jobs.Dispose();
        quickPivots.Dispose();

        for (var i = 0; i < outQueuesLength; i++)
        {
            outQueues[i].Dispose();
        }

        for (var i = 0; i < outArrays.Length; i++)
        {
            outArrays[i].Dispose();
        }

        outQueues.Dispose();
        outArrays.Dispose();
    }

    [BurstCompile]
    private partial struct CullJob : IJobEntity
    {
        public float XLeft; // Left most cull position
        public float XRight; // Right most cull position
        public float YTop; // Top most cull position
        public float YBottom; // Bottom most cull position

        public NativeQueue<RenderData> NativeQueue;

        public void Execute(ref LocalToWorld localToWorld, ref SpriteSheetAnimation animationData)
        {
            var positionX = localToWorld.Position.x;
            if (!(positionX > XLeft) || !(positionX < XRight))
            {
                // Unit is not within horizontal view-bounds. No need to render.
                return;
            }

            var positionY = localToWorld.Position.y;
            if (!(positionY > YBottom) || !(positionY < YTop))
            {
                // Unit is not within vertical view-bounds. No need to render.
                return;
            }

            var renderData = new RenderData
            {
                Position = localToWorld.Position,
                Matrix = animationData.Matrix,
                Uv = animationData.Uv
            };

            NativeQueue.Enqueue(renderData);
        }
    }

    [BurstCompile]
    private struct QuickSortJob : IJob
    {
        [ReadOnly] public float Pivot;

        public NativeQueue<RenderData> InQueue;
        public NativeQueue<RenderData> OutQueue1;
        public NativeQueue<RenderData> OutQueue2;

        public void Execute()
        {
            while (InQueue.Count > 0)
            {
                var renderData = InQueue.Dequeue();
                if (renderData.Position.y > Pivot)
                {
                    OutQueue1.Enqueue(renderData);
                }
                else
                {
                    OutQueue2.Enqueue(renderData);
                }
            }
        }
    }

    [BurstCompile]
    private struct NativeQueueToArrayJob : IJob
    {
        public NativeQueue<RenderData> NativeQueue;
        public NativeArray<RenderData> NativeArray;

        public void Execute()
        {
            var index = 0;
            while (NativeQueue.TryDequeue(out var renderData))
            {
                NativeArray[index] = renderData;
                index++;
            }
        }
    }

    [BurstCompile]
    private struct SortByPositionParallelJob : IJob
    {
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<RenderData> SharedNativeArray;

        [ReadOnly] public int StartIndex;
        [ReadOnly] public int EndIndex;

        public void Execute()
        {
            for (var i = StartIndex; i < EndIndex; i++)
            {
                for (var j = i + 1; j < EndIndex; j++)
                {
                    var pos = SharedNativeArray[i].Position.y;
                    var otherPos = SharedNativeArray[j].Position.y;
                    if (pos < otherPos)
                    {
                        // Swap
                        (SharedNativeArray[i], SharedNativeArray[j]) = (SharedNativeArray[j], SharedNativeArray[i]);
                    }
                }
            }
        }
    }

    [BurstCompile]
    private struct FillArraysJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<RenderData> SortedArray;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Matrix4x4> MatrixArray;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector4> UvArray;

        public void Execute(int index)
        {
            var renderData = SortedArray[index];
            MatrixArray[index] = renderData.Matrix;
            UvArray[index] = renderData.Uv;
        }
    }

    private struct RenderData
    {
        public float3 Position;
        public Matrix4x4 Matrix;
        public Vector4 Uv;
    }
}
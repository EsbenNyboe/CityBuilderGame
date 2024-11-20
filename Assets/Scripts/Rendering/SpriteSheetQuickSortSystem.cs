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

        var spriteSheetsToSort = GetDataToSort(ref state, out var yTop, out var yBottom);
        var visibleEntitiesTotal = spriteSheetsToSort.Count;

        var splitTimes = 4;
        var splitJobCount = (int)math.pow(2, splitTimes) - 1;

        var arrayOfQueues = InitializeQuickSortQueues(splitJobCount, spriteSheetsToSort);

        var quickPivots = GetArrayOfPivots(splitJobCount, yBottom, yTop);
        var jobBatchSizes = GetJobBatchSizes(splitTimes, splitJobCount);

        QuickSortQueuesToVerticalSections(arrayOfQueues, quickPivots, jobBatchSizes);
        var arrayOfArrays = ConvertQueuesToArrays(arrayOfQueues);

        GetSingletonDataContainers(ref state, visibleEntitiesTotal, out var spriteMatrixArray, out var spriteUvArray);

        var sharedNativeArray = MergeArraysIntoOneSharedArray(visibleEntitiesTotal, arrayOfArrays,
            out var startIndexes,
            out var endIndexes);

        var dependency = ScheduleBubbleSortingOfEachSection(sharedNativeArray, startIndexes, endIndexes);
        dependency = ScheduleWritingToDataContainers(sharedNativeArray, spriteMatrixArray, spriteUvArray, dependency,
            visibleEntitiesTotal);
        state.Dependency = dependency;
    }


    private NativeQueue<RenderData> GetDataToSort(ref SystemState state, out float yTop, out float yBottom)
    {
        var cameraInformation = SystemAPI.GetSingleton<CameraInformation>();
        var cameraPosition = cameraInformation.CameraPosition;
        var screenRatio = cameraInformation.ScreenRatio;
        var orthographicSize = cameraInformation.OrthographicSize;

        var cullBuffer = 1f; // We add some buffer, so culling is not noticable
        var cameraSizeX = orthographicSize * screenRatio + cullBuffer;
        var cameraSizeY = orthographicSize + cullBuffer;

        var xLeft = cameraPosition.x - cameraSizeX;
        var xRight = cameraPosition.x + cameraSizeX;
        yTop = cameraPosition.y + cameraSizeY;
        yBottom = cameraPosition.y - cameraSizeY;

        // Filter out all units that are outside the field of view
        var spriteSheetsToSort = new NativeQueue<RenderData>(Allocator.TempJob);
        new CullJob
        {
            XLeft = xLeft,
            XRight = xRight,
            YTop = yTop,
            YBottom = yBottom,
            NativeQueue = spriteSheetsToSort
        }.Run(_entityQuery);

        return spriteSheetsToSort;
    }

    private static NativeArray<NativeQueue<RenderData>> InitializeQuickSortQueues(int splitJobCount,
        NativeQueue<RenderData> spriteSheetsToSort)
    {
        var outQueues = new NativeArray<NativeQueue<RenderData>>(1 + splitJobCount * 2, Allocator.Temp);
        outQueues[0] = spriteSheetsToSort;
        return outQueues;
    }

    private static NativeArray<float> GetArrayOfPivots(int splitJobCount, float yBottom, float yTop)
    {
        var quickPivots = new NativeArray<float>(splitJobCount, Allocator.Temp);

        var normalizedPivotInterval = 1f;
        var normalizedPivot = 0.5f;
        var normalizedPivotSum = normalizedPivot;
        for (var i = 0; i < splitJobCount; i++)
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

        return quickPivots;
    }

    private static NativeArray<int> GetJobBatchSizes(int splitTimes, int splitJobCount)
    {
        var jobBatchSizes = new NativeArray<int>(splitTimes, Allocator.Temp);
        var jobBatchSizeSum = 0;
        var jobBatchSize = 1;
        var jobBatchIndex = 0;

        for (var i = 0; i < splitJobCount; i++)
        {
            if (i == jobBatchSizeSum + jobBatchSize - 1)
            {
                // jobBatches follow the pattern of 1, 2, 4, 8, etc...
                jobBatchSizes[jobBatchIndex] = jobBatchSize;
                jobBatchSizeSum += jobBatchSize;
                jobBatchSize *= 2;
                jobBatchIndex++;
            }
        }

        return jobBatchSizes;
    }

    private static void QuickSortQueuesToVerticalSections(NativeArray<NativeQueue<RenderData>> outQueues,
        NativeArray<float> quickPivots,
        NativeArray<int> jobBatchSizes)
    {
        var jobBatch = new NativeList<JobHandle>(Allocator.Temp);
        var jobIndex = 0;
        foreach (var batchSize in jobBatchSizes)
        {
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
                jobBatch.Add(quickSortJob.Schedule());
                jobIndex++;
            }

            JobHandle.CompleteAll(jobBatch.AsArray());
            jobBatch.Clear();
        }

        jobBatch.Dispose();
        jobBatchSizes.Dispose();
        quickPivots.Dispose();
    }

    private static NativeArray<NativeArray<RenderData>> ConvertQueuesToArrays(
        NativeArray<NativeQueue<RenderData>> arrayOfQueues)
    {
        var outQueuesLength = arrayOfQueues.Length;
        var jobs = new NativeArray<JobHandle>(outQueuesLength, Allocator.Temp);
        var arrayOfArrays = new NativeArray<NativeArray<RenderData>>(outQueuesLength, Allocator.Temp);
        for (var i = 0; i < outQueuesLength; i++)
        {
            var nativeQueueToArrayJob = new NativeQueueToArrayJob
            {
                NativeQueue = arrayOfQueues[i],
                NativeArray = arrayOfArrays[i] = new NativeArray<RenderData>(arrayOfQueues[i].Count, Allocator.TempJob)
            };
            jobs[i] = nativeQueueToArrayJob.Schedule();
        }

        JobHandle.CompleteAll(jobs);
        jobs.Dispose();

        for (var i = 0; i < arrayOfQueues.Length; i++)
        {
            arrayOfQueues[i].Dispose();
        }

        arrayOfQueues.Dispose();

        return arrayOfArrays;
    }

    private void GetSingletonDataContainers(ref SystemState state, int visibleEntitiesTotal,
        out NativeArray<Matrix4x4> spriteMatrixArray,
        out NativeArray<Vector4> spriteUvArray)
    {
        // Clear data
        var singleton = SystemAPI.GetSingletonRW<SpriteSheetSortingManager>();
        singleton.ValueRW.SpriteMatrixArray.Dispose();
        singleton.ValueRW.SpriteUvArray.Dispose();
        singleton.ValueRW.SpriteMatrixArray = new NativeArray<Matrix4x4>(visibleEntitiesTotal, Allocator.TempJob);
        singleton.ValueRW.SpriteUvArray = new NativeArray<Vector4>(visibleEntitiesTotal, Allocator.TempJob);
        spriteMatrixArray = singleton.ValueRO.SpriteMatrixArray;
        spriteUvArray = singleton.ValueRO.SpriteUvArray;
    }

    private static NativeArray<RenderData> MergeArraysIntoOneSharedArray(int visibleEntitiesTotal,
        NativeArray<NativeArray<RenderData>> outArrays,
        out NativeArray<int> startIndexes, out NativeArray<int> endIndexes)
    {
        var sharedNativeArray = new NativeArray<RenderData>(visibleEntitiesTotal, Allocator.TempJob);
        startIndexes = new NativeArray<int>(outArrays.Length, Allocator.TempJob);
        endIndexes = new NativeArray<int>(outArrays.Length, Allocator.TempJob);
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
            outArrays[i].Dispose();
        }

        outArrays.Dispose();

        return sharedNativeArray;
    }

    private static JobHandle ScheduleBubbleSortingOfEachSection(NativeArray<RenderData> sharedNativeArray,
        NativeArray<int> startIndexes, NativeArray<int> endIndexes)
    {
        var jobs = new NativeArray<JobHandle>(startIndexes.Length, Allocator.Temp);
        for (var i = 0; i < jobs.Length; i++)
        {
            var sortByPositionJob = new SortByPositionParallelJob
            {
                SharedNativeArray = sharedNativeArray,
                StartIndex = startIndexes[i],
                EndIndex = endIndexes[i]
            };
            jobs[i] = sortByPositionJob.Schedule();
        }

        var dependency = JobHandle.CombineDependencies(jobs);
        startIndexes.Dispose(dependency);
        endIndexes.Dispose(dependency);
        jobs.Dispose();
        return dependency;
    }

    private static JobHandle ScheduleWritingToDataContainers(NativeArray<RenderData> sharedNativeArray,
        NativeArray<Matrix4x4> spriteMatrixArray,
        NativeArray<Vector4> spriteUvArray, JobHandle dependency, int visibleEntitiesTotal)
    {
        var fillArraysJob = new FillArraysJob
        {
            SortedArray = sharedNativeArray,
            MatrixArray = spriteMatrixArray,
            UvArray = spriteUvArray
        };
        dependency = fillArraysJob.Schedule(visibleEntitiesTotal, 10, dependency);
        sharedNativeArray.Dispose(dependency);
        return dependency;
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
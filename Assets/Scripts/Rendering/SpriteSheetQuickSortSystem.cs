using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rendering
{
    public partial struct SpriteSheetQuickSortSystem : ISystem
    {
        private EntityQuery _entityQuery;

        public void OnCreate(ref SystemState state)
        {
            _entityQuery = state.GetEntityQuery(ComponentType.ReadOnly<SpriteSheetAnimation>(),
                ComponentType.ReadOnly<LocalToWorld>());
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("Camera is null");
                throw new Exception();
            }

            var cullBuffer = 1f; // We add some buffer, so culling is not noticable
            float3 cameraPosition = camera.transform.position;
            var screenRatio = Screen.width / (float)Screen.height;
            var cameraSizeX = camera.orthographicSize * screenRatio + cullBuffer;
            var cameraSizeY = camera.orthographicSize + cullBuffer;

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
                normalizedPivotSum += normalizedPivotInterval;
                if (normalizedPivotSum >= 1)
                {
                    normalizedPivot *= 0.5f;
                    normalizedPivotInterval *= 0.5f;
                    normalizedPivotSum = normalizedPivot;
                }
            }

            var jobBatches = new NativeArray<NativeArray<JobHandle>>(splitTimes, Allocator.Temp);

            var finishedJobBatches = 0;
            var jobBatchSize = 1;
            var jobBatchIndex = 0;
            for (var i = 0; i < quickPivots.Length; i++)
            {
                Debug.Log(jobBatchSize + " Jobs: " + i);
                if (i == finishedJobBatches + jobBatchSize - 1)
                {
                    // jobBatches follow the pattern of 1, 2, 4, 8, etc...
                    jobBatches[jobBatchIndex] = new NativeArray<JobHandle>(jobBatchSize, Allocator.TempJob);
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

            var jobIndex = 0;
            for (var i = 0; i < jobBatches.Length; i++)
            {
                var batchSize = jobBatches[i].Length;
                for (var j = 0; j < batchSize; j++)
                {
                    var pivot = quickPivots[jobIndex];
                    var quickSortJob = new QuickSortJob
                    {
                        Pivot = pivot,
                        InQueue = outQueues[jobIndex],
                        OutQueue1 = outQueues[jobIndex + batchSize + j] =
                            new NativeQueue<RenderData>(Allocator.TempJob),
                        OutQueue2 = outQueues[jobIndex + batchSize + j + 1] =
                            new NativeQueue<RenderData>(Allocator.TempJob)
                    };
                    var dependency = i > 0 ? JobHandle.CombineDependencies(jobBatches[i - 1]) : default;
                    var jobBatch = jobBatches[i];
                    jobBatch[j] = quickSortJob.Schedule(dependency);
                    jobBatches[i] = jobBatch;
                    jobIndex++;
                }
            }

            var queueJobs = new NativeArray<JobHandle>(jobBatches.Length, Allocator.TempJob);
            for (var i = 0; i < jobBatches.Length; i++)
            {
                queueJobs[i] = JobHandle.CombineDependencies(jobBatches[i]);
            }

            JobHandle.CompleteAll(queueJobs);
            queueJobs.Dispose();

            var outArrays = new NativeArray<NativeArray<RenderData>>(outQueues.Length, Allocator.Temp);
            jobIndex = 0;
            for (var i = 0; i < jobBatches.Length; i++)
            {
                var batchSize = jobBatches[i].Length;
                for (var j = 0; j < batchSize; j++)
                {
                    var nativeQueueToArrayJob = new NativeQueueToArrayJob
                    {
                        NativeQueue = outQueues[jobIndex],
                        NativeArray = outArrays[jobIndex] =
                            new NativeArray<RenderData>(outQueues[jobIndex].Count, Allocator.TempJob)
                    };
                    var dependency = i > 0 ? JobHandle.CombineDependencies(jobBatches[i - 1]) : default;
                    var jobBatch = jobBatches[i];
                    jobBatch[j] = nativeQueueToArrayJob.Schedule(dependency);
                    jobBatches[i] = jobBatch;
                    jobIndex++;
                }
            }

            var jobs = new NativeArray<JobHandle>(jobBatches.Length, Allocator.Temp);
            for (var index = 0; index < jobBatches.Length; index++)
            {
                jobs[index] = JobHandle.CombineDependencies(jobBatches[index]);
                jobBatches[index].Dispose();
            }

            JobHandle.CompleteAll(jobs);
            jobs.Dispose();

            for (var i = 0; i < outQueues.Length; i++)
            {
                while (outQueues[i].Count > 0)
                {
                    var renderData = outQueues[i].Dequeue();
                    Debug.Log("Queue " + i + " contains: " + renderData.Position.y);
                }

                outQueues[i].Dispose();
            }

            outQueues.Dispose();

            for (var i = 0; i < outArrays.Length; i++)
            {
                outArrays[i].Dispose();
            }

            outArrays.Dispose();
            quickPivots.Dispose();
            jobBatches.Dispose();
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
                    if (renderData.Position.y < Pivot)
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

        public struct RenderData
        {
            public float3 Position;
            public Matrix4x4 Matrix;
            public Vector4 Uv;
        }
    }
}
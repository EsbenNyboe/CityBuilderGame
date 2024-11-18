using System;
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
        public void OnUpdate(ref SystemState state)
        {
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

            for (var i = 0; i < quickPivots.Length; i++)
            {
                Debug.Log("Quick pivot " + i + ": " + quickPivots[i]);
            }

            quickPivots.Dispose();
            jobBatches.Dispose();
            // var queueStructList = new NativeList<QueueStruct>(Allocator.TempJob);


            // CULL

            // THEN SORT
        }

        private partial struct CullAndSort : IJobEntity
        {
            public float XLeft; // Left most cull position
            public float XRight; // Right most cull position
            public float YTop1; // Top most cull position
            public float YTop2; // Second slice from top
            public float YTop3; // Third slice from top
            public float YTop4; // Fourth slice from top
            public float YBottom; // Bottom most cull position

            public NativeQueue<RenderData> NativeQueue1;
            public NativeQueue<RenderData> NativeQueue2;
            public NativeQueue<RenderData> NativeQueue3;
            public NativeQueue<RenderData> NativeQueue4;

            public void Execute(ref LocalToWorld localToWorld, ref SpriteSheetAnimation animationData)
            {
                var positionX = localToWorld.Position.x;
                if (!(positionX > XLeft) || !(positionX < XRight))
                {
                    // Unit is not within horizontal view-bounds. No need to render.
                    return;
                }

                var positionY = localToWorld.Position.y;
                if (!(positionY > YBottom) || !(positionY < YTop1))
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

                if (positionY < YTop4)
                {
                    NativeQueue4.Enqueue(renderData);
                }
                else if (positionY < YTop3)
                {
                    NativeQueue3.Enqueue(renderData);
                }
                else if (positionY < YTop2)
                {
                    NativeQueue2.Enqueue(renderData);
                }
                else
                {
                    NativeQueue1.Enqueue(renderData);
                }
            }
        }

        public struct RenderData
        {
            public float3 Position;
            public Matrix4x4 Matrix;
            public Vector4 Uv;
        }

        public struct QueueStruct
        {
            public NativeQueue<RenderData> Queue;
        }
    }
}
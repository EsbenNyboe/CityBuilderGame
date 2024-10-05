using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(SpriteSheetAnimationSystem))]
public partial class SpriteSheetRendererSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<SpriteSheetAnimationData>();
    }

    protected override void OnUpdate()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("Camera is null");
            return;
        }

        var materialPropertyBlock = new MaterialPropertyBlock();
        var mesh = SpriteSheetRendererManager.Instance.TestMesh;
        var material = SpriteSheetRendererManager.Instance.TestMaterial;

        var entityQuery = GetEntityQuery(typeof(SpriteSheetAnimationData), typeof(LocalTransform));

        // Create sliced queues of the data, before sorting
        var nativeQueue_1 = new NativeQueue<RenderData>(Allocator.TempJob);
        var nativeQueue_2 = new NativeQueue<RenderData>(Allocator.TempJob);

        float3 cameraPosition = camera.transform.position;
        var yBottom = cameraPosition.y - camera.orthographicSize;
        var yTop_1 = cameraPosition.y + camera.orthographicSize;
        var yTop_2 = cameraPosition.y + 0f;

        var cullAndSortJob = new CullAndSort
        {
            yTop_1 = yTop_1,
            yTop_2 = yTop_2,
            yBottom = yBottom,
            NativeQueue_1 = nativeQueue_1,
            NativeQueue_2 = nativeQueue_2
            // ToConcurrent doesn't exist... is it necessary?
            // NativeQueue_1 = nativeQueue_1.ToConcurrent(),
            // NativeQueue_2 = nativeQueue_2.ToConcurrent(),
        };
        cullAndSortJob.Run(entityQuery);

        // Convert sliced queues into sliced arrays
        var nativeArray_1 = new NativeArray<RenderData>(nativeQueue_1.Count, Allocator.TempJob);
        var nativeArray_2 = new NativeArray<RenderData>(nativeQueue_2.Count, Allocator.TempJob);

        var jobHandleArray = new NativeArray<JobHandle>(2, Allocator.TempJob);

        var nativeQueueToArrayJob_1 = new NativeQueueToArrayJob
        {
            NativeQueue = nativeQueue_1,
            NativeArray = nativeArray_1
        };
        jobHandleArray[0] = nativeQueueToArrayJob_1.Schedule();
        var nativeQueueToArrayJob_2 = new NativeQueueToArrayJob
        {
            NativeQueue = nativeQueue_2,
            NativeArray = nativeArray_2
        };
        jobHandleArray[1] = nativeQueueToArrayJob_2.Schedule();
        JobHandle.CompleteAll(jobHandleArray);

        nativeQueue_1.Dispose();
        nativeQueue_2.Dispose();

        // Sort the sliced arrays
        var sortByPosition_1 = new SortByPositionJob
        {
            SortArray = nativeArray_1
        };
        jobHandleArray[0] = sortByPosition_1.Schedule();

        var sortByPositionJob_2 = new SortByPositionJob
        {
            SortArray = nativeArray_2
        };
        jobHandleArray[1] = sortByPositionJob_2.Schedule();
        JobHandle.CompleteAll(jobHandleArray);

        // Grab sliced arrays and merge them into one array per domain
        var visibleEntityTotal = nativeArray_1.Length + nativeArray_2.Length;
        var matrixArray = new NativeArray<Matrix4x4>(visibleEntityTotal, Allocator.TempJob);
        var uvArray = new NativeArray<Vector4>(visibleEntityTotal, Allocator.TempJob);

        var fillArraysParallelJob_1 = new FillArraysParallelJob
        {
            NativeArray = nativeArray_1,
            MatrixArray = matrixArray,
            UvArray = uvArray,
            StartIndex = 0
        };
        jobHandleArray[0] = fillArraysParallelJob_1.Schedule(nativeArray_1.Length, 10);

        var fillArraysParallelJob_2 = new FillArraysParallelJob
        {
            NativeArray = nativeArray_2,
            MatrixArray = matrixArray,
            UvArray = uvArray,
            StartIndex = nativeArray_1.Length
        };
        jobHandleArray[1] = fillArraysParallelJob_2.Schedule(nativeArray_2.Length, 10);

        JobHandle.CompleteAll(jobHandleArray);
        // NativeArray<Matrix4x4>.Copy(matrixArray, startIndex, matrixArray2, 0, length);

        materialPropertyBlock.SetVectorArray("_MainTex_UV", uvArray.ToArray());
        Graphics.DrawMeshInstanced(mesh, 0, material, matrixArray.ToArray(), matrixArray.Length, materialPropertyBlock);

        nativeArray_1.Dispose();
        nativeArray_2.Dispose();
        jobHandleArray.Dispose();
    }

    private struct RenderData
    {
        public Entity Entity;
        public float3 Position;
        public Matrix4x4 Matrix;
        public Vector4 Uv;
    }

    [BurstCompile]
    private partial struct CullAndSort : IJobEntity
    {
        public float yTop_1; // Top most cull position
        public float yTop_2; // Second slice from top
        public float yBottom; // Bottom most cull position

        // Concurrent is unknown... is it necessary to add the "Concurrent" part? 
        // public NativeQueue<RenderData>.Concurrent NativeQueue_1;
        public NativeQueue<RenderData> NativeQueue_1;
        public NativeQueue<RenderData> NativeQueue_2;

        public void Execute(Entity entity, ref LocalTransform localTransform, ref SpriteSheetAnimationData animationData)
        {
            var positionY = localTransform.Position.y;
            if (!(positionY > yBottom) || !(positionY < yTop_1))
            {
                // Not valid position
            }

            var renderData = new RenderData
            {
                Entity = entity,
                Position = localTransform.Position,
                Matrix = animationData.Matrix,
                Uv = animationData.Uv
            };

            if (positionY < yTop_2)
            {
                NativeQueue_2.Enqueue(renderData);
            }
            else
            {
                NativeQueue_1.Enqueue(renderData);
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
            RenderData renderData;
            while (NativeQueue.TryDequeue(out renderData))
            {
                NativeArray[index] = renderData;
                index++;
            }
        }
    }

    [BurstCompile]
    private struct SortByPositionJob : IJob
    {
        public NativeArray<RenderData> SortArray;

        public void Execute()
        {
            for (var i = 0; i < SortArray.Length; i++)
            {
                for (var j = i + 1; j < SortArray.Length; j++)
                {
                    if (SortArray[i].Position.y < SortArray[j].Position.y)
                    {
                        // Swap
                        var localTransform = SortArray[i];
                        SortArray[i] = SortArray[j];
                        SortArray[j] = localTransform;
                    }
                }
            }
        }
    }

    [BurstCompile]
    private struct FillArraysParallelJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<RenderData> NativeArray;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Matrix4x4> MatrixArray;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector4> UvArray;

        [ReadOnly] public int StartIndex;

        public void Execute(int index)
        {
            var renderData = NativeArray[index];
            MatrixArray[StartIndex + index] = renderData.Matrix;
            UvArray[StartIndex + index] = renderData.Uv;
        }
    }
}
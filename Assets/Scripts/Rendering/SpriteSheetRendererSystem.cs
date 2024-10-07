using System;
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
        RequireForUpdate<SpriteSheetAnimation>();
    }

    protected override void OnUpdate()
    {
        var unitMesh = SpriteSheetRendererManager.Instance.UnitMesh;
        var unitMaterial = SpriteSheetRendererManager.Instance.UnitMaterial;
        HandleMeshRendering(unitMesh, unitMaterial);
    }

    private void HandleMeshRendering(Mesh unitMesh, Material unitWalkingMaterial)
    {
        // Create sliced queues of the data, before sorting
        CreateSlicedQueues(out var nativeQueue_1, out var nativeQueue_2);

        var jobHandleArray = new NativeArray<JobHandle>(2, Allocator.TempJob);

        // Convert sliced queues into sliced arrays
        ConvertQueuesToArrays(jobHandleArray, nativeQueue_1, nativeQueue_2, out var nativeArray_1, out var nativeArray_2);

        // Sort the sliced arrays
        SortSlicedArrays(jobHandleArray, nativeArray_1, nativeArray_2);

        // Grab sliced arrays and merge them into one array per domain
        MergeSlicedArrays(jobHandleArray, out var matrixArray, out var uvArray, nativeArray_1, nativeArray_2);

        // Setup uv's to select sprite-frame, then draw mesh for all instances
        DrawMesh(unitMesh, unitWalkingMaterial, uvArray, matrixArray);

        jobHandleArray.Dispose();
        matrixArray.Dispose();
        uvArray.Dispose();
        nativeQueue_1.Dispose();
        nativeQueue_2.Dispose();
        nativeArray_1.Dispose();
        nativeArray_2.Dispose();
    }

    private void CreateSlicedQueues(out NativeQueue<RenderData> nativeQueue_1, out NativeQueue<RenderData> nativeQueue_2)
    {
        var entityQuery = GetEntityQuery(typeof(SpriteSheetAnimation), typeof(LocalToWorld));

        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("Camera is null");
            throw new Exception();
        }

        nativeQueue_1 = new NativeQueue<RenderData>(Allocator.TempJob);
        nativeQueue_2 = new NativeQueue<RenderData>(Allocator.TempJob);

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
        };
        cullAndSortJob.Run(entityQuery);
    }

    private static void ConvertQueuesToArrays(NativeArray<JobHandle> jobHandleArray, NativeQueue<RenderData> nativeQueue_1,
        NativeQueue<RenderData> nativeQueue_2,
        out NativeArray<RenderData> nativeArray_1, out NativeArray<RenderData> nativeArray_2)
    {
        nativeArray_1 = new NativeArray<RenderData>(nativeQueue_1.Count, Allocator.TempJob);
        nativeArray_2 = new NativeArray<RenderData>(nativeQueue_2.Count, Allocator.TempJob);

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
    }

    private static void SortSlicedArrays(NativeArray<JobHandle> jobHandleArray, NativeArray<RenderData> nativeArray_1,
        NativeArray<RenderData> nativeArray_2)
    {
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
    }

    private static void MergeSlicedArrays(NativeArray<JobHandle> jobHandleArray, out NativeArray<Matrix4x4> matrixArray,
        out NativeArray<Vector4> uvArray, NativeArray<RenderData> nativeArray_1, NativeArray<RenderData> nativeArray_2)
    {
        var visibleEntityTotal = nativeArray_1.Length + nativeArray_2.Length;
        matrixArray = new NativeArray<Matrix4x4>(visibleEntityTotal, Allocator.TempJob);
        uvArray = new NativeArray<Vector4>(visibleEntityTotal, Allocator.TempJob);

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
    }

    private static void DrawMesh(Mesh mesh, Material material, NativeArray<Vector4> uvArray, NativeArray<Matrix4x4> matrixArray)
    {
        var materialPropertyBlock = new MaterialPropertyBlock();

        var sliceCount = 1023;
        var matrixInstancedArray = new Matrix4x4[sliceCount];
        var uvInstancedArray = new Vector4[sliceCount];

        // Should we use animationDataArray instead of matrixArray? Assuming we don't have to, since they should have the same length, right?
        for (var i = 0; i < matrixArray.Length; i += sliceCount)
        {
            var sliceSize = math.min(matrixArray.Length - i, sliceCount);
            NativeArray<Matrix4x4>.Copy(matrixArray, i, matrixInstancedArray, 0, sliceSize);
            NativeArray<Vector4>.Copy(uvArray, i, uvInstancedArray, 0, sliceSize);

            materialPropertyBlock.SetVectorArray("_MainTex_UV", uvInstancedArray);
            Graphics.DrawMeshInstanced(mesh, 0, material, matrixInstancedArray, sliceSize, materialPropertyBlock);
        }
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

        public NativeQueue<RenderData> NativeQueue_1;
        public NativeQueue<RenderData> NativeQueue_2;

        public void Execute(Entity entity, ref LocalToWorld localToWorld, ref SpriteSheetAnimation animationData)
        {
            var positionY = localToWorld.Position.y;
            if (!(positionY > yBottom) || !(positionY < yTop_1))
            {
                // Not valid position
            }

            var renderData = new RenderData
            {
                Entity = entity,
                Position = localToWorld.Position,
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
                        (SortArray[i], SortArray[j]) = (SortArray[j], SortArray[i]);
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
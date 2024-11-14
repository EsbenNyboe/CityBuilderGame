using System;
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

[UpdateInGroup(typeof(PreRenderingSystemGroup))]
[BurstCompile]
public partial struct SpriteSheetSortingSystem : ISystem
{
    private EntityQuery _entityQuery;

    public void OnCreate(ref SystemState state)
    {
        state.EntityManager.AddComponent<SpriteSheetSortingManager>(state.SystemHandle);
        _entityQuery = state.GetEntityQuery(ComponentType.ReadOnly<SpriteSheetAnimation>(),
            ComponentType.ReadOnly<LocalToWorld>());
    }

    public void OnUpdate(ref SystemState state)
    {
        HandleMeshSorting(ref state);
    }

    private void HandleMeshSorting(ref SystemState state)
    {
        // Create sliced queues of the data, before sorting
        CreateSlicedQueues(ref state, out var nativeQueue_1, out var nativeQueue_2, out var nativeQueue_3,
            out var nativeQueue_4);

        var jobHandleArray = new NativeArray<JobHandle>(4, Allocator.TempJob);

        // Convert sliced queues into sliced arrays
        ConvertQueuesToArrays(jobHandleArray, nativeQueue_1, nativeQueue_2, nativeQueue_3, nativeQueue_4,
            out var nativeArray_1,
            out var nativeArray_2, out var nativeArray_3, out var nativeArray_4);

        // Sort the sliced arrays
        SortSlicedArrays(jobHandleArray, nativeArray_1, nativeArray_2, nativeArray_3, nativeArray_4);

        // Grab sliced arrays and merge them into one array per domain
        MergeSlicedArrays(jobHandleArray, out var matrixArray, out var uvArray, nativeArray_1, nativeArray_2,
            nativeArray_3, nativeArray_4);

        var spriteSheetSortingManager = SystemAPI.GetComponent<SpriteSheetSortingManager>(state.SystemHandle);
        spriteSheetSortingManager.SpriteMatrixArray.Dispose();
        spriteSheetSortingManager.SpriteUvArray.Dispose();
        spriteSheetSortingManager.SpriteMatrixArray = matrixArray;
        spriteSheetSortingManager.SpriteUvArray = uvArray;
        SystemAPI.SetComponent(state.SystemHandle, spriteSheetSortingManager);

        jobHandleArray.Dispose();
        nativeQueue_1.Dispose();
        nativeQueue_2.Dispose();
        nativeQueue_3.Dispose();
        nativeQueue_4.Dispose();
        nativeArray_1.Dispose();
        nativeArray_2.Dispose();
        nativeArray_3.Dispose();
        nativeArray_4.Dispose();
    }

    public void OnDestroy(ref SystemState state)
    {
        var spriteSheetSortingManager = SystemAPI.GetComponent<SpriteSheetSortingManager>(state.SystemHandle);
        spriteSheetSortingManager.SpriteMatrixArray.Dispose();
        spriteSheetSortingManager.SpriteUvArray.Dispose();
    }

    private void CreateSlicedQueues(ref SystemState state, out NativeQueue<RenderData> nativeQueue_1,
        out NativeQueue<RenderData> nativeQueue_2,
        out NativeQueue<RenderData> nativeQueue_3, out NativeQueue<RenderData> nativeQueue_4)
    {
        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("Camera is null");
            throw new Exception();
        }

        nativeQueue_1 = new NativeQueue<RenderData>(Allocator.TempJob);
        nativeQueue_2 = new NativeQueue<RenderData>(Allocator.TempJob);
        nativeQueue_3 = new NativeQueue<RenderData>(Allocator.TempJob);
        nativeQueue_4 = new NativeQueue<RenderData>(Allocator.TempJob);

        var cullBuffer = 1f; // We add some buffer, so culling is not noticable
        float3 cameraPosition = camera.transform.position;
        var screenRatio = Screen.width / (float)Screen.height;
        var cameraSizeX = camera.orthographicSize * screenRatio + cullBuffer;
        var cameraSizeY = camera.orthographicSize + cullBuffer;

        var xLeft = cameraPosition.x - cameraSizeX;
        var xRight = cameraPosition.x + cameraSizeX;

        var yTop_1 = cameraPosition.y + cameraSizeY;
        var yTop_2 = cameraPosition.y + cameraSizeY * 0.5f;
        var yTop_3 = cameraPosition.y;
        var yTop_4 = cameraPosition.y - cameraSizeY * 0.5f;
        var yBottom = cameraPosition.y - cameraSizeY;

        var cullAndSortJob = new CullAndSort
        {
            xLeft = xLeft,
            xRight = xRight,
            yBottom = yBottom,
            yTop_1 = yTop_1,
            yTop_2 = yTop_2,
            yTop_3 = yTop_3,
            yTop_4 = yTop_4,
            NativeQueue_1 = nativeQueue_1,
            NativeQueue_2 = nativeQueue_2,
            NativeQueue_3 = nativeQueue_3,
            NativeQueue_4 = nativeQueue_4
        };
        cullAndSortJob.Run(_entityQuery);
    }

    private static void ConvertQueuesToArrays(NativeArray<JobHandle> jobHandleArray,
        NativeQueue<RenderData> nativeQueue_1,
        NativeQueue<RenderData> nativeQueue_2, NativeQueue<RenderData> nativeQueue_3,
        NativeQueue<RenderData> nativeQueue_4,
        out NativeArray<RenderData> nativeArray_1, out NativeArray<RenderData> nativeArray_2,
        out NativeArray<RenderData> nativeArray_3,
        out NativeArray<RenderData> nativeArray_4)

    {
        nativeArray_1 = new NativeArray<RenderData>(nativeQueue_1.Count, Allocator.TempJob);
        nativeArray_2 = new NativeArray<RenderData>(nativeQueue_2.Count, Allocator.TempJob);
        nativeArray_3 = new NativeArray<RenderData>(nativeQueue_3.Count, Allocator.TempJob);
        nativeArray_4 = new NativeArray<RenderData>(nativeQueue_4.Count, Allocator.TempJob);

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
        var nativeQueueToArrayJob_3 = new NativeQueueToArrayJob
        {
            NativeQueue = nativeQueue_3,
            NativeArray = nativeArray_3
        };
        jobHandleArray[2] = nativeQueueToArrayJob_3.Schedule();
        var nativeQueueToArrayJob_4 = new NativeQueueToArrayJob
        {
            NativeQueue = nativeQueue_4,
            NativeArray = nativeArray_4
        };
        jobHandleArray[3] = nativeQueueToArrayJob_4.Schedule();
        JobHandle.CompleteAll(jobHandleArray);
    }

    private static void SortSlicedArrays(NativeArray<JobHandle> jobHandleArray, NativeArray<RenderData> nativeArray_1,
        NativeArray<RenderData> nativeArray_2, NativeArray<RenderData> nativeArray_3,
        NativeArray<RenderData> nativeArray_4)
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
        var sortByPosition_3 = new SortByPositionJob
        {
            SortArray = nativeArray_3
        };
        jobHandleArray[2] = sortByPosition_3.Schedule();

        var sortByPositionJob_4 = new SortByPositionJob
        {
            SortArray = nativeArray_4
        };
        jobHandleArray[3] = sortByPositionJob_4.Schedule();
        JobHandle.CompleteAll(jobHandleArray);
    }

    private static void MergeSlicedArrays(NativeArray<JobHandle> jobHandleArray, out NativeArray<Matrix4x4> matrixArray,
        out NativeArray<Vector4> uvArray, NativeArray<RenderData> nativeArray_1, NativeArray<RenderData> nativeArray_2,
        NativeArray<RenderData> nativeArray_3, NativeArray<RenderData> nativeArray_4)
    {
        var visibleEntityTotal =
            nativeArray_1.Length + nativeArray_2.Length + nativeArray_3.Length + nativeArray_4.Length;
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

        var fillArraysParallelJob_3 = new FillArraysParallelJob
        {
            NativeArray = nativeArray_3,
            MatrixArray = matrixArray,
            UvArray = uvArray,
            StartIndex = nativeArray_1.Length + nativeArray_2.Length
        };
        jobHandleArray[2] = fillArraysParallelJob_3.Schedule(nativeArray_3.Length, 10);

        var fillArraysParallelJob_4 = new FillArraysParallelJob
        {
            NativeArray = nativeArray_4,
            MatrixArray = matrixArray,
            UvArray = uvArray,
            StartIndex = nativeArray_1.Length + nativeArray_2.Length + nativeArray_3.Length
        };
        jobHandleArray[3] = fillArraysParallelJob_4.Schedule(nativeArray_4.Length, 10);
        JobHandle.CompleteAll(jobHandleArray);
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
        public float xLeft; // Left most cull position
        public float xRight; // Right most cull position
        public float yTop_1; // Top most cull position
        public float yTop_2; // Second slice from top
        public float yTop_3; // Third slice from top
        public float yTop_4; // Fourth slice from top
        public float yBottom; // Bottom most cull position

        public NativeQueue<RenderData> NativeQueue_1;
        public NativeQueue<RenderData> NativeQueue_2;
        public NativeQueue<RenderData> NativeQueue_3;
        public NativeQueue<RenderData> NativeQueue_4;

        public void Execute(Entity entity, ref LocalToWorld localToWorld, ref SpriteSheetAnimation animationData)
        {
            var positionX = localToWorld.Position.x;
            if (!(positionX > xLeft) || !(positionX < xRight))
            {
                // Unit is not within horizontal view-bounds. No need to render.
                return;
            }

            var positionY = localToWorld.Position.y;
            if (!(positionY > yBottom) || !(positionY < yTop_1))
            {
                // Unit is not within vertical view-bounds. No need to render.
                return;
            }

            var renderData = new RenderData
            {
                Entity = entity,
                Position = localToWorld.Position,
                Matrix = animationData.Matrix,
                Uv = animationData.Uv
            };

            if (positionY < yTop_4)
            {
                NativeQueue_4.Enqueue(renderData);
            }
            else if (positionY < yTop_3)
            {
                NativeQueue_3.Enqueue(renderData);
            }
            else if (positionY < yTop_2)
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
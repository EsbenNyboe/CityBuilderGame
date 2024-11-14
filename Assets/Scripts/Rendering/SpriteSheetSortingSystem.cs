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

[UpdateInGroup(typeof(PreRenderingSystemGroup), OrderLast = true)]
[BurstCompile]
public partial struct SpriteSheetSortingSystem : ISystem
{
    private EntityQuery _entityQuery;

    public void OnCreate(ref SystemState state)
    {
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

    public void OnUpdate(ref SystemState state)
    {
        HandleMeshSorting(ref state);
    }

    private void HandleMeshSorting(ref SystemState state)
    {
        state.WorldUnmanaged.GetExistingSystemState<LocalToWorldSystem>().CompleteDependency();

        // Create sliced queues of the data, before sorting
        CreateSlicedQueues(ref state, out var nativeQueue1, out var nativeQueue2, out var nativeQueue3,
            out var nativeQueue4);

        var jobHandleArray = new NativeArray<JobHandle>(4, Allocator.TempJob);

        // Convert sliced queues into sliced arrays
        ConvertQueuesToArrays(jobHandleArray, nativeQueue1, nativeQueue2, nativeQueue3, nativeQueue4,
            out var nativeArray1, out var nativeArray2, out var nativeArray3, out var nativeArray4);
        var queueDisposalDependency = JobHandle.CombineDependencies(jobHandleArray);
        nativeQueue1.Dispose(queueDisposalDependency);
        nativeQueue2.Dispose(queueDisposalDependency);
        nativeQueue3.Dispose(queueDisposalDependency);
        nativeQueue4.Dispose(queueDisposalDependency);

        // Sort the sliced arrays
        SortSlicedArrays(jobHandleArray, nativeArray1, nativeArray2, nativeArray3, nativeArray4);

        // Grab sliced arrays and merge them into one array per domain
        MergeSlicedArrays(ref state, jobHandleArray, nativeArray1, nativeArray2, nativeArray3, nativeArray4);
        var finalDependency = JobHandle.CombineDependencies(jobHandleArray);

        // Dispose the rest
        jobHandleArray.Dispose(finalDependency);
        nativeArray1.Dispose(finalDependency);
        nativeArray2.Dispose(finalDependency);
        nativeArray3.Dispose(finalDependency);
        nativeArray4.Dispose(finalDependency);
        state.Dependency = finalDependency;
    }

    public void OnDestroy(ref SystemState state)
    {
        var spriteSheetSortingManager = SystemAPI.GetSingleton<SpriteSheetSortingManager>();
        spriteSheetSortingManager.SpriteMatrixArray.Dispose();
        spriteSheetSortingManager.SpriteUvArray.Dispose();
    }

    private void CreateSlicedQueues(ref SystemState state, out NativeQueue<RenderData> nativeQueue1,
        out NativeQueue<RenderData> nativeQueue2,
        out NativeQueue<RenderData> nativeQueue3, out NativeQueue<RenderData> nativeQueue4)
    {
        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("Camera is null");
            throw new Exception();
        }

        nativeQueue1 = new NativeQueue<RenderData>(Allocator.TempJob);
        nativeQueue2 = new NativeQueue<RenderData>(Allocator.TempJob);
        nativeQueue3 = new NativeQueue<RenderData>(Allocator.TempJob);
        nativeQueue4 = new NativeQueue<RenderData>(Allocator.TempJob);

        var cullBuffer = 1f; // We add some buffer, so culling is not noticable
        float3 cameraPosition = camera.transform.position;
        var screenRatio = Screen.width / (float)Screen.height;
        var cameraSizeX = camera.orthographicSize * screenRatio + cullBuffer;
        var cameraSizeY = camera.orthographicSize + cullBuffer;

        var xLeft = cameraPosition.x - cameraSizeX;
        var xRight = cameraPosition.x + cameraSizeX;

        var yTop1 = cameraPosition.y + cameraSizeY;
        var yTop2 = cameraPosition.y + cameraSizeY * 0.5f;
        var yTop3 = cameraPosition.y;
        var yTop4 = cameraPosition.y - cameraSizeY * 0.5f;
        var yBottom = cameraPosition.y - cameraSizeY;

        var cullAndSortJob = new CullAndSort
        {
            XLeft = xLeft,
            XRight = xRight,
            YBottom = yBottom,
            YTop1 = yTop1,
            YTop2 = yTop2,
            YTop3 = yTop3,
            YTop4 = yTop4,
            NativeQueue1 = nativeQueue1,
            NativeQueue2 = nativeQueue2,
            NativeQueue3 = nativeQueue3,
            NativeQueue4 = nativeQueue4
        };
        cullAndSortJob.Run(_entityQuery);
    }

    private static void ConvertQueuesToArrays(NativeArray<JobHandle> jobHandleArray,
        NativeQueue<RenderData> nativeQueue1,
        NativeQueue<RenderData> nativeQueue2, NativeQueue<RenderData> nativeQueue3,
        NativeQueue<RenderData> nativeQueue4,
        out NativeArray<RenderData> nativeArray1, out NativeArray<RenderData> nativeArray2,
        out NativeArray<RenderData> nativeArray3,
        out NativeArray<RenderData> nativeArray4)

    {
        nativeArray1 = new NativeArray<RenderData>(nativeQueue1.Count, Allocator.TempJob);
        nativeArray2 = new NativeArray<RenderData>(nativeQueue2.Count, Allocator.TempJob);
        nativeArray3 = new NativeArray<RenderData>(nativeQueue3.Count, Allocator.TempJob);
        nativeArray4 = new NativeArray<RenderData>(nativeQueue4.Count, Allocator.TempJob);

        var nativeQueueToArrayJob1 = new NativeQueueToArrayJob
        {
            NativeQueue = nativeQueue1,
            NativeArray = nativeArray1
        };
        jobHandleArray[0] = nativeQueueToArrayJob1.Schedule();
        var nativeQueueToArrayJob2 = new NativeQueueToArrayJob
        {
            NativeQueue = nativeQueue2,
            NativeArray = nativeArray2
        };
        jobHandleArray[1] = nativeQueueToArrayJob2.Schedule();
        var nativeQueueToArrayJob3 = new NativeQueueToArrayJob
        {
            NativeQueue = nativeQueue3,
            NativeArray = nativeArray3
        };
        jobHandleArray[2] = nativeQueueToArrayJob3.Schedule();
        var nativeQueueToArrayJob4 = new NativeQueueToArrayJob
        {
            NativeQueue = nativeQueue4,
            NativeArray = nativeArray4
        };
        jobHandleArray[3] = nativeQueueToArrayJob4.Schedule();
        JobHandle.CompleteAll(jobHandleArray);
    }

    private static void SortSlicedArrays(NativeArray<JobHandle> jobHandleArray, NativeArray<RenderData> nativeArray1,
        NativeArray<RenderData> nativeArray2, NativeArray<RenderData> nativeArray3,
        NativeArray<RenderData> nativeArray4)
    {
        var sortByPosition1 = new SortByPositionJob
        {
            SortArray = nativeArray1
        };
        jobHandleArray[0] = sortByPosition1.Schedule();
        var sortByPositionJob2 = new SortByPositionJob
        {
            SortArray = nativeArray2
        };
        jobHandleArray[1] = sortByPositionJob2.Schedule();
        var sortByPosition3 = new SortByPositionJob
        {
            SortArray = nativeArray3
        };
        jobHandleArray[2] = sortByPosition3.Schedule();

        var sortByPositionJob4 = new SortByPositionJob
        {
            SortArray = nativeArray4
        };
        jobHandleArray[3] = sortByPositionJob4.Schedule();
    }

    private void MergeSlicedArrays(ref SystemState state, NativeArray<JobHandle> jobHandleArray,
        NativeArray<RenderData> nativeArray1, NativeArray<RenderData> nativeArray2,
        NativeArray<RenderData> nativeArray3, NativeArray<RenderData> nativeArray4)
    {
        var visibleEntityTotal =
            nativeArray1.Length + nativeArray2.Length + nativeArray3.Length + nativeArray4.Length;

        var singleton = SystemAPI.GetSingletonRW<SpriteSheetSortingManager>();
        singleton.ValueRW.SpriteMatrixArray.Dispose();
        singleton.ValueRW.SpriteUvArray.Dispose();
        singleton.ValueRW.SpriteMatrixArray = new NativeArray<Matrix4x4>(visibleEntityTotal, Allocator.TempJob);
        singleton.ValueRW.SpriteUvArray = new NativeArray<Vector4>(visibleEntityTotal, Allocator.TempJob);

        var matrixArray = singleton.ValueRO.SpriteMatrixArray;
        var uvArray = singleton.ValueRO.SpriteUvArray;

        var jobDependency = JobHandle.CombineDependencies(jobHandleArray);
        var fillArraysParallelJob1 = new FillArraysParallelJob
        {
            NativeArray = nativeArray1,
            MatrixArray = matrixArray,
            UvArray = uvArray,
            StartIndex = 0
        };
        jobHandleArray[0] = fillArraysParallelJob1.Schedule(nativeArray1.Length, 10, jobDependency);

        var fillArraysParallelJob2 = new FillArraysParallelJob
        {
            NativeArray = nativeArray2,
            MatrixArray = matrixArray,
            UvArray = uvArray,
            StartIndex = nativeArray1.Length
        };
        jobHandleArray[1] = fillArraysParallelJob2.Schedule(nativeArray2.Length, 10, jobDependency);

        var fillArraysParallelJob3 = new FillArraysParallelJob
        {
            NativeArray = nativeArray3,
            MatrixArray = matrixArray,
            UvArray = uvArray,
            StartIndex = nativeArray1.Length + nativeArray2.Length
        };
        jobHandleArray[2] = fillArraysParallelJob3.Schedule(nativeArray3.Length, 10, jobDependency);

        var fillArraysParallelJob4 = new FillArraysParallelJob
        {
            NativeArray = nativeArray4,
            MatrixArray = matrixArray,
            UvArray = uvArray,
            StartIndex = nativeArray1.Length + nativeArray2.Length + nativeArray3.Length
        };
        jobHandleArray[3] = fillArraysParallelJob4.Schedule(nativeArray4.Length, 10, jobDependency);
    }

    private struct RenderData
    {
        public float3 Position;
        public Matrix4x4 Matrix;
        public Vector4 Uv;
    }

    [BurstCompile]
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
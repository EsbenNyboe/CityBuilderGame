using Rendering;
using UnitState;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct WorldSpriteSheetSortingManager : IComponentData
{
    public NativeArray<Matrix4x4> SpriteMatrixArray;
    public NativeArray<Vector4> SpriteUvArray;
}

[UpdateInGroup(typeof(PreRenderingSystemGroup), OrderLast = true)]
[BurstCompile]
public partial struct WorldSpriteSheetSortingManagerSystem : ISystem
{
    private EntityQuery _unitQuery;
    private EntityQuery _droppedItemQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<InventoryEnum>();
        state.RequireForUpdate<SortingJobConfig>();
        state.RequireForUpdate<WorldSpriteSheetManager>();
        state.RequireForUpdate<CameraInformation>();
        var singletonEntity = state.EntityManager.CreateSingleton<WorldSpriteSheetSortingManager>();
        SystemAPI.SetComponent(singletonEntity, new WorldSpriteSheetSortingManager
        {
            SpriteMatrixArray = new NativeArray<Matrix4x4>(1, Allocator.TempJob),
            SpriteUvArray = new NativeArray<Vector4>(1, Allocator.TempJob)
        });
        _unitQuery = state.GetEntityQuery(ComponentType.ReadOnly<WorldSpriteSheetAnimation>(),
            ComponentType.ReadOnly<LocalToWorld>(), ComponentType.ReadOnly<Inventory>());
        _droppedItemQuery = state.GetEntityQuery(ComponentType.ReadOnly<DroppedItem>(), ComponentType.ReadOnly<LocalTransform>());

        state.RequireForUpdate<BeginPresentationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<WorldSpriteSheetSortingManager>();
    }

    public void OnDestroy(ref SystemState state)
    {
        state.Dependency.Complete();
        var singleton = SystemAPI.GetSingletonRW<WorldSpriteSheetSortingManager>();
        singleton.ValueRW.SpriteMatrixArray.Dispose();
        singleton.ValueRW.SpriteUvArray.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency.Complete();
        var worldSpriteSheetManager = SystemAPI.GetSingleton<WorldSpriteSheetManager>();
        if (!worldSpriteSheetManager.IsInitialized())
        {
            return;
        }

        var spriteSheetsToSort =
            GetDataToSort(ref state, worldSpriteSheetManager, out var yTop, out var yBottom, out var inventoryRenderDataQueue);
        var visibleUnitsCount = spriteSheetsToSort.Count;
        var visibleItemsCount = inventoryRenderDataQueue.Count;

        var sortingTest = SystemAPI.GetSingleton<SortingJobConfig>();
        var sectionCount = (int)math.pow(sortingTest.SectionsPerSplitJob, sortingTest.SplitJobCount);
        var pivotCount = sectionCount - 1;
        var queueCount = 1;
        var jobIndex = 0;
        while (jobIndex < sortingTest.SplitJobCount)
        {
            var outputQueues = (int)math.pow(sortingTest.SectionsPerSplitJob, jobIndex + 1);
            queueCount += outputQueues;
            jobIndex++;
        }

        var arrayOfQueues = new NativeArray<QueueContainer>(queueCount, Allocator.Temp);
        arrayOfQueues[0] = new QueueContainer
        {
            SortingQueue = spriteSheetsToSort
        };
        for (var i = 1; i < arrayOfQueues.Length; i++)
        {
            arrayOfQueues[i] = new QueueContainer
            {
                SortingQueue = new NativeQueue<RenderData>(Allocator.TempJob)
            };
        }

        var batchSectionCounts = new NativeArray<int>(sortingTest.SplitJobCount, Allocator.Temp);
        for (var i = 0; i < batchSectionCounts.Length; i++)
        {
            batchSectionCounts[i] = (int)math.pow(sortingTest.SectionsPerSplitJob, i + 1);
            // Debug.Log("Job section count " + i + ": " + batchSectionCounts[i]);
        }

        var pivots = GetArrayOfPivots(pivotCount, batchSectionCounts, yBottom, yTop);
        var jobBatchSizes = new NativeArray<int>(sortingTest.SplitJobCount, Allocator.Temp);
        jobBatchSizes[0] = 1;
        for (var i = 1; i < sortingTest.SplitJobCount; i++)
        {
            jobBatchSizes[i] = batchSectionCounts[i - 1];
        }

        QuickSortQueuesToVerticalSections(arrayOfQueues, pivots, jobBatchSizes, sortingTest.SectionsPerSplitJob - 1);

        var arrayOfArrays = ConvertQueuesToArrays(arrayOfQueues);

        var sharedSortingArray = MergeArraysIntoOneSharedArray(visibleUnitsCount, arrayOfArrays,
            out var startIndexes,
            out var endIndexes);

        var dependency = ScheduleBubbleSortingOfEachSection(sharedSortingArray, startIndexes, endIndexes);

        dependency = MergeSortedArrayWithInventoryData(dependency, ref state, sharedSortingArray, inventoryRenderDataQueue,
            worldSpriteSheetManager, out var allEntitiesToRenderArray);
        GetSingletonDataContainers(ref state, visibleUnitsCount + visibleItemsCount,
            out var spriteMatrixArray, out var spriteUvArray);
        dependency = ScheduleWritingToDataContainers(allEntitiesToRenderArray, spriteMatrixArray, spriteUvArray,
            dependency, visibleUnitsCount + visibleItemsCount);
        state.Dependency = dependency;
    }

    private JobHandle MergeSortedArrayWithInventoryData(JobHandle dependency,
        ref SystemState state,
        NativeArray<RenderData> sharedSortingArray,
        NativeQueue<InventoryRenderData> inventoryRenderDataQueue,
        WorldSpriteSheetManager worldSpriteSheetManager, out NativeArray<RenderData> mergedArray)
    {
        // Convert this to a job
        var inventoryItemsToRender = inventoryRenderDataQueue.Count;
        var inventoryRenderDataHashMap =
            new NativeParallelHashMap<Entity, InventoryRenderData>(inventoryItemsToRender, Allocator.TempJob);

        while (inventoryRenderDataQueue.Count > 0)
        {
            var inventoryRenderData = inventoryRenderDataQueue.Dequeue();
            inventoryRenderDataHashMap.Add(inventoryRenderData.Entity, inventoryRenderData);
        }

        inventoryRenderDataQueue.Dispose();

        mergedArray =
            new NativeArray<RenderData>(sharedSortingArray.Length + inventoryItemsToRender, Allocator.TempJob);

        var enumLength = SystemAPI.GetSingleton<InventoryEnum>().ItemEnumLength;

        var inventoryItemSpriteSheetColumnLookup = new NativeArray<int>(enumLength, Allocator.TempJob);
        var inventoryItemSpriteSheetRowLookup = new NativeArray<int>(enumLength, Allocator.TempJob);
        for (var i = 0; i < enumLength; i++)
        {
            worldSpriteSheetManager.GetInventoryItemCoordinates((InventoryItem)i, out var column, out var row);
            inventoryItemSpriteSheetColumnLookup[i] = column;
            inventoryItemSpriteSheetRowLookup[i] = row;
        }

        var newDependency = new MergeSortedArrayWithInventoryDataJob
        {
            MergedArray = mergedArray,
            InventoryRenderDataHashMap = inventoryRenderDataHashMap,
            SortedArray = sharedSortingArray,
            ColumnScale = worldSpriteSheetManager.ColumnScale,
            RowScale = worldSpriteSheetManager.RowScale,
            InventoryItemSpriteSheetColumnLookup = inventoryItemSpriteSheetColumnLookup,
            InventoryItemSpriteSheetRowLookup = inventoryItemSpriteSheetRowLookup
        }.Schedule(dependency);

        inventoryRenderDataHashMap.Dispose(newDependency);
        sharedSortingArray.Dispose(newDependency);
        inventoryItemSpriteSheetColumnLookup.Dispose(newDependency);
        inventoryItemSpriteSheetRowLookup.Dispose(newDependency);

        return newDependency;
    }

    private NativeQueue<RenderData> GetDataToSort(ref SystemState state, WorldSpriteSheetManager worldSpriteSheetManager,
        out float yTop, out float yBottom, out NativeQueue<InventoryRenderData> inventoryRenderDataQueue)
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
        inventoryRenderDataQueue = new NativeQueue<InventoryRenderData>(Allocator.TempJob);
        new CullJob
        {
            XLeft = xLeft,
            XRight = xRight,
            YTop = yTop,
            YBottom = yBottom,
            NativeQueue = spriteSheetsToSort,
            InventoryRenderDataQueue = inventoryRenderDataQueue
        }.Run(_unitQuery);

        new CullJobOnDroppedItems
        {
            XLeft = xLeft,
            XRight = xRight,
            YTop = yTop,
            YBottom = yBottom,
            WorldSpriteSheetManager = worldSpriteSheetManager,
            NativeQueue = spriteSheetsToSort
        }.Run(_droppedItemQuery);

        return spriteSheetsToSort;
    }

    private static NativeArray<float> GetArrayOfPivots(int pivotCount, NativeArray<int> batchSectionCounts, float yBottom, float yTop)
    {
        var pivots = new NativeArray<float>(pivotCount, Allocator.TempJob);
        var pivotIndex = 0;
        for (var i = 0; i < batchSectionCounts.Length; i++)
        {
            var batchSectionCount = batchSectionCounts[i];
            var fractionPerSection = 1f / batchSectionCount;
            for (var j = 1; j < batchSectionCount; j++) // Start at 1, because pivots are 1 less than sections
            {
                var previousBatchContainsPivot = false;
                for (var k = 0; k < i; k++)
                {
                    if (j % batchSectionCounts[k] == 0)
                    {
                        previousBatchContainsPivot = true;
                    }
                }

                if (previousBatchContainsPivot)
                {
                    continue;
                }

                var offset = 1f - fractionPerSection * j;
                pivots[pivotIndex] = yBottom + offset * (yTop - yBottom);
                // Debug.Log("Pivot " + pivotIndex + ": " + pivots[pivotIndex]);
                pivotIndex++;
            }
        }

        return pivots;
    }

    private static void QuickSortQueuesToVerticalSections(NativeArray<QueueContainer> outQueues,
        NativeArray<float> quickPivots,
        NativeArray<int> jobBatchSizes,
        int pivotCount)
    {
        var jobBatch = new NativeList<JobHandle>(Allocator.Temp);
        var jobIndex = 0;
        var pivotIndex = 0;
        var outputIndex = 1;
        for (var i = 0; i < jobBatchSizes.Length; i++)
        {
            var batchSize = jobBatchSizes[i];
            for (var j = 0; j < batchSize; j++)
            {
                if (outQueues[jobIndex].SortingQueue.Count > 0)
                {
                    jobBatch.Add(new QuickSortJob
                    {
                        InQueue = outQueues[jobIndex].SortingQueue,
                        Pivots = quickPivots,
                        PivotsStartIndex = pivotIndex,
                        PivotCount = pivotCount,
                        SortingQueues = outQueues,
                        OutputStartIndex = outputIndex
                    }.Schedule());
                }

                jobIndex++;
                pivotIndex += pivotCount;
                outputIndex += pivotCount + 1;
            }

            JobHandle.CompleteAll(jobBatch.AsArray());
            jobBatch.Clear();
        }

        jobBatch.Dispose();
        jobBatchSizes.Dispose();
        quickPivots.Dispose();
    }

    private static NativeArray<NativeArray<RenderData>> ConvertQueuesToArrays(
        NativeArray<QueueContainer> arrayOfQueues)
    {
        var outQueuesLength = arrayOfQueues.Length;
        var jobs = new NativeList<JobHandle>(outQueuesLength, Allocator.Temp);
        var arrayOfArrays = new NativeArray<NativeArray<RenderData>>(outQueuesLength, Allocator.Temp);
        for (var i = 0; i < arrayOfArrays.Length; i++)
        {
            arrayOfArrays[i] = new NativeArray<RenderData>(arrayOfQueues[i].SortingQueue.Count, Allocator.TempJob);
        }

        for (var i = 0; i < outQueuesLength; i++)
        {
            if (arrayOfQueues[i].SortingQueue.Count > 0)
            {
                jobs.Add(new NativeQueueToArrayJob
                {
                    NativeQueue = arrayOfQueues[i].SortingQueue,
                    NativeArray = arrayOfArrays[i]
                }.Schedule());
            }
        }

        JobHandle.CompleteAll(jobs.AsArray());
        jobs.Dispose();

        for (var i = 0; i < arrayOfQueues.Length; i++)
        {
            arrayOfQueues[i].SortingQueue.Dispose();
        }

        arrayOfQueues.Dispose();

        return arrayOfArrays;
    }

    private void GetSingletonDataContainers(ref SystemState state, int visibleEntitiesTotal,
        out NativeArray<Matrix4x4> spriteMatrixArray,
        out NativeArray<Vector4> spriteUvArray)
    {
        // Clear data
        var singleton = SystemAPI.GetSingletonRW<WorldSpriteSheetSortingManager>();
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
                var renderData = outArrays[i][j];
                sharedNativeArray[sharedIndex] = renderData;
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
        var jobs = new NativeList<JobHandle>(startIndexes.Length, Allocator.Temp);
        for (var i = 0; i < startIndexes.Length; i++)
        {
            if (endIndexes[i] > startIndexes[i])
            {
                jobs.Add(new SortByPositionParallelJob
                {
                    SharedNativeArray = sharedNativeArray,
                    StartIndex = startIndexes[i],
                    EndIndex = endIndexes[i]
                }.Schedule());
            }
        }

        var dependency = JobHandle.CombineDependencies(jobs.AsArray());
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
        public NativeQueue<InventoryRenderData> InventoryRenderDataQueue;

        public void Execute(in Entity Entity, in LocalToWorld localToWorld, in WorldSpriteSheetAnimation animationData,
            in Inventory inventory)
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
                Entity = Entity,
                Position = localToWorld.Position,
                Matrix = animationData.Matrix,
                Uv = animationData.Uv
            };

            NativeQueue.Enqueue(renderData);
            if (inventory.CurrentItem != InventoryItem.None)
            {
                InventoryRenderDataQueue.Enqueue(new InventoryRenderData
                {
                    Entity = Entity,
                    Item = inventory.CurrentItem,
                    Amount = 1
                });
            }
        }
    }

    [BurstCompile]
    private partial struct CullJobOnDroppedItems : IJobEntity
    {
        [ReadOnly] public float XLeft; // Left most cull position
        [ReadOnly] public float XRight; // Right most cull position
        [ReadOnly] public float YTop; // Top most cull position
        [ReadOnly] public float YBottom; // Bottom most cull position

        [ReadOnly] [NativeDisableContainerSafetyRestriction]
        public WorldSpriteSheetManager WorldSpriteSheetManager;

        public NativeQueue<RenderData> NativeQueue;

        public void Execute(in Entity Entity, in DroppedItem droppedItem, in LocalTransform localTransform)
        {
            var position = localTransform.Position;
            var positionX = position.x;
            if (!(positionX > XLeft) || !(positionX < XRight))
            {
                // Item is not within horizontal view-bounds. No need to render.
                return;
            }

            var positionY = position.y;
            if (!(positionY > YBottom) || !(positionY < YTop))
            {
                // Item is not within vertical view-bounds. No need to render.
                return;
            }

            WorldSpriteSheetManager.GetInventoryItemCoordinates(droppedItem.Item, out var column, out var row);
            var columnScale = WorldSpriteSheetManager.ColumnScale;
            var rowScale = WorldSpriteSheetManager.RowScale;
            const float groundOffset = 0.4f;
            var renderPosition = new float3(position.x, position.y - groundOffset, position.z);
            var renderData = new RenderData
            {
                Entity = Entity,
                Position = position,
                Matrix = Matrix4x4.TRS(renderPosition, quaternion.identity, Vector3.one),
                Uv = new Vector4(columnScale, rowScale, column * columnScale, row * rowScale)
            };

            NativeQueue.Enqueue(renderData);
        }
    }

    private struct QueueContainer
    {
        public NativeQueue<RenderData> SortingQueue;
    }

    [BurstCompile]
    private struct QuickSortJob : IJob
    {
        public NativeQueue<RenderData> InQueue;

        [ReadOnly] public NativeArray<float> Pivots;
        public int PivotsStartIndex;
        public int PivotCount;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<QueueContainer> SortingQueues;

        public int OutputStartIndex;

        public void Execute()
        {
            QuickSortLogic(InQueue, PivotsStartIndex, PivotCount, SortingQueues, OutputStartIndex, Pivots);
        }
    }

    private static void QuickSortLogic(NativeQueue<RenderData> inQueue,
        int pivotsStartIndex,
        int pivotCount,
        NativeArray<QueueContainer> sortingQueues,
        int outputStartIndex,
        NativeArray<float> pivots)
    {
        while (inQueue.Count > 0)
        {
            var renderData = inQueue.Dequeue();
            var dataIsSorted = false;

            for (var i = pivotsStartIndex; i < pivotsStartIndex + pivotCount; i++)
            {
                if (renderData.Position.y > pivots[i])
                {
                    sortingQueues[outputStartIndex + i - pivotsStartIndex].SortingQueue.Enqueue(renderData);
                    dataIsSorted = true;
                    break;
                }
            }

            if (!dataIsSorted)
            {
                sortingQueues[outputStartIndex + pivotCount].SortingQueue.Enqueue(renderData);
            }
        }
    }

    [BurstCompile]
    private struct QuickSortJob_OLD : IJob
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
    private struct MergeSortedArrayWithInventoryDataJob : IJob
    {
        public NativeArray<RenderData> MergedArray;
        [ReadOnly] public NativeParallelHashMap<Entity, InventoryRenderData> InventoryRenderDataHashMap;
        [ReadOnly] public NativeArray<RenderData> SortedArray;
        [ReadOnly] public float ColumnScale;
        [ReadOnly] public float RowScale;
        [ReadOnly] public NativeArray<int> InventoryItemSpriteSheetColumnLookup;
        [ReadOnly] public NativeArray<int> InventoryItemSpriteSheetRowLookup;

        public void Execute()
        {
            var mergedArrayIndex = 0;
            for (var i = 0; i < SortedArray.Length; i++)
            {
                var renderData = SortedArray[i];
                MergedArray[mergedArrayIndex] = renderData;
                mergedArrayIndex++;

                if (InventoryRenderDataHashMap.TryGetValue(renderData.Entity, out var itemData))
                {
                    var inventoryUv = new Vector4(ColumnScale, RowScale, 0, 0)
                    {
                        z = ColumnScale * InventoryItemSpriteSheetColumnLookup[(int)itemData.Item],
                        w = RowScale * InventoryItemSpriteSheetRowLookup[(int)itemData.Item]
                    };
                    // TODO: Is it possible to modify the y-position of a matrix? (for stacking the inventory items)
                    var inventoryMatrix = renderData.Matrix;
                    var itemRenderData = new RenderData
                    {
                        Matrix = inventoryMatrix,
                        Uv = inventoryUv
                    };

                    MergedArray[mergedArrayIndex] = itemRenderData;
                    mergedArrayIndex++;
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
        public Entity Entity;
        public float3 Position;
        public Matrix4x4 Matrix;
        public Vector4 Uv;
        public InventoryItem InventoryItem;
    }

    private struct InventoryRenderData
    {
        public Entity Entity;
        public InventoryItem Item;
        public int Amount;
    }
}
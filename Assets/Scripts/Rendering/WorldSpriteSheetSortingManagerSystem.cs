using GridEntityNS;
using Inventory;
using Rendering.Cullable;
using SystemGroups;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rendering
{
    public struct WorldSpriteSheetSortingManager : ICleanupComponentData
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
        private EntityQuery _gridEntityQuery;

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
            _unitQuery = state.GetEntityQuery(ComponentType.ReadOnly<WorldSpriteSheetState>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<InventoryState>());
            _droppedItemQuery = state.GetEntityQuery(ComponentType.ReadOnly<DroppedItem>(),
                ComponentType.ReadOnly<LocalTransform>());
            _gridEntityQuery = state.GetEntityQuery(ComponentType.ReadOnly<WorldSpriteSheetState>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<GridEntity>(),
                ComponentType.ReadOnly<Renderable>());

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

            var sortingTest = SystemAPI.GetSingleton<SortingJobConfig>();
            var sectionCount = (int)math.pow(sortingTest.SectionsPerSplitJob, sortingTest.SplitJobCount);
            var pivotCount = sectionCount - 1;

            var queueCount = 0;
            var batchQueueCounts = new NativeArray<int>(sortingTest.SplitJobCount, Allocator.Temp);
            for (var i = 0; i < sortingTest.SplitJobCount; i++)
            {
                var outputQueuesInBatch = (int)math.pow(sortingTest.SectionsPerSplitJob, i + 1);
                queueCount += outputQueuesInBatch;
                batchQueueCounts[i] = outputQueuesInBatch;
            }

            var jobBatchSizes = new NativeArray<int>(sortingTest.SplitJobCount, Allocator.Temp);
            jobBatchSizes[0] = 1;
            for (var i = 1; i < sortingTest.SplitJobCount; i++)
            {
                jobBatchSizes[i] = batchQueueCounts[i - 1];
            }

            var sortingQueues = new NativeArray<QueueContainer>(queueCount, Allocator.TempJob);
            for (var i = 0; i < queueCount; i++)
            {
                sortingQueues[i] = new QueueContainer
                {
                    SortingQueue = new NativeQueue<RenderData>(Allocator.TempJob)
                };
            }

            GetCameraBounds(ref state, out var yTop, out var yBottom, out var xLeft, out var xRight);
            var pivots = GetArrayOfPivots(pivotCount, batchQueueCounts, yBottom, yTop);
            var pivotsPerQuickSort = sortingTest.SectionsPerSplitJob - 1;
            GetDataToSort(ref state, worldSpriteSheetManager, yTop, yBottom, xLeft, xRight, out var inventoryRenderDataQueue,
                pivots, pivotsPerQuickSort, sortingQueues);

            var visibleItemsCount = inventoryRenderDataQueue.Count;
            var visibleUnitsCount = 0;
            for (var i = 0; i < batchQueueCounts[0]; i++)
            {
                visibleUnitsCount += sortingQueues[i].SortingQueue.Count;
            }

            QuickSortQueuesToVerticalSections(sortingQueues, batchQueueCounts, pivots, jobBatchSizes, pivotsPerQuickSort);

            var sortingArrays = ConvertQueuesToArrays(sortingQueues);

            var sharedSortingArray = MergeArraysIntoOneSharedArray(visibleUnitsCount, sortingArrays,
                out var startIndexes,
                out var endIndexes);

            var dependency = ScheduleBubbleSortingOfEachSection(sharedSortingArray, startIndexes, endIndexes);

            dependency = MergeSortedArrayWithInventoryData(dependency, ref state, sharedSortingArray, inventoryRenderDataQueue,
                worldSpriteSheetManager, out var finalArrayOfRenderData);
            GetSingletonDataContainers(ref state, visibleUnitsCount + visibleItemsCount,
                out var spriteMatrixArray, out var spriteUvArray);
            dependency = ScheduleWritingToDataContainers(finalArrayOfRenderData, spriteMatrixArray, spriteUvArray,
                dependency, visibleUnitsCount + visibleItemsCount);
            state.Dependency = dependency;
        }

        private JobHandle MergeSortedArrayWithInventoryData(JobHandle dependency,
            ref SystemState state,
            NativeArray<RenderData> sharedSortingArray,
            NativeQueue<InventoryRenderData> inventoryRenderDataQueue,
            WorldSpriteSheetManager worldSpriteSheetManager, out NativeArray<RenderData> finalArrayOfRenderData)
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

            finalArrayOfRenderData =
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
                MergedArray = finalArrayOfRenderData,
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

        private void GetDataToSort(ref SystemState state, WorldSpriteSheetManager worldSpriteSheetManager,
            float yTop, float yBottom, float xLeft, float xRight, out NativeQueue<InventoryRenderData> inventoryRenderDataQueue,
            NativeArray<float> pivots, int pivotCount,
            NativeArray<QueueContainer> sortingQueues)
        {
            inventoryRenderDataQueue = new NativeQueue<InventoryRenderData>(Allocator.TempJob);

            new CullJob
            {
                XLeft = xLeft,
                XRight = xRight,
                YTop = yTop,
                YBottom = yBottom,
                InventoryRenderDataQueue = inventoryRenderDataQueue,
                Pivots = pivots,
                PivotCount = pivotCount,
                SortingQueues = sortingQueues
            }.Run(_unitQuery);

            new CullJobOnDroppedItems
            {
                XLeft = xLeft,
                XRight = xRight,
                YTop = yTop,
                YBottom = yBottom,
                WorldSpriteSheetManager = worldSpriteSheetManager,
                Pivots = pivots,
                PivotCount = pivotCount,
                SortingQueues = sortingQueues
            }.Run(_droppedItemQuery);

            new CullJobOnGridEntities
            {
                Pivots = pivots,
                PivotCount = pivotCount,
                SortingQueues = sortingQueues
            }.Run(_gridEntityQuery);
        }

        private void GetCameraBounds(ref SystemState state, out float yTop, out float yBottom, out float xLeft, out float xRight)
        {
            var cameraInformation = SystemAPI.GetSingleton<CameraInformation>();
            var cameraPosition = cameraInformation.CameraPosition;
            var screenRatio = cameraInformation.ScreenRatio;
            var orthographicSize = cameraInformation.OrthographicSize;

            var cullBuffer = 1f; // We add some buffer, so culling is not noticable
            var cameraSizeX = orthographicSize * screenRatio + cullBuffer;
            var cameraSizeY = orthographicSize + cullBuffer;

            xLeft = cameraPosition.x - cameraSizeX;
            xRight = cameraPosition.x + cameraSizeX;
            yTop = cameraPosition.y + cameraSizeY;
            yBottom = cameraPosition.y - cameraSizeY;
        }

        private static NativeArray<float> GetArrayOfPivots(int pivotCount, NativeArray<int> batchQueueCounts, float yBottom, float yTop)
        {
            var pivots = new NativeArray<float>(pivotCount, Allocator.TempJob);
            var pivotIndex = 0;
            for (var i = 0; i < batchQueueCounts.Length; i++)
            {
                var batchSectionCount = batchQueueCounts[i];
                var fractionPerSection = 1f / batchSectionCount;
                for (var j = 1; j < batchSectionCount; j++) // Start at 1, because pivots are 1 less than sections
                {
                    var previousBatchContainsPivot = false;
                    for (var k = 0; k < i; k++)
                    {
                        if (j % batchQueueCounts[k] == 0)
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

        private static void QuickSortQueuesToVerticalSections(NativeArray<QueueContainer> sortingQueues,
            NativeArray<int> batchQueueCounts,
            NativeArray<float> pivots,
            NativeArray<int> jobBatchSizes,
            int pivotCount)
        {
            var jobBatch = new NativeList<JobHandle>(Allocator.Temp);
            var jobIndex = 0;
            // PivotIndex, OutputIndex, and batch skip to the second iteration,
            // because we already did a quick-sort during culling
            var pivotIndex = pivotCount;
            var outputIndex = batchQueueCounts[0];
            for (var batch = 1; batch < jobBatchSizes.Length; batch++)
            {
                for (var job = 0; job < jobBatchSizes[batch]; job++)
                {
                    if (sortingQueues[jobIndex].SortingQueue.Count > 0)
                    {
                        jobBatch.Add(new QuickSortJob
                        {
                            InQueue = sortingQueues[jobIndex].SortingQueue,
                            Pivots = pivots,
                            PivotsStartIndex = pivotIndex,
                            PivotCount = pivotCount,
                            SortingQueues = sortingQueues,
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
            pivots.Dispose();
            batchQueueCounts.Dispose();
        }

        private static NativeArray<NativeArray<RenderData>> ConvertQueuesToArrays(
            NativeArray<QueueContainer> sortingQueues)
        {
            var outQueuesLength = sortingQueues.Length;
            var jobs = new NativeList<JobHandle>(outQueuesLength, Allocator.Temp);
            var sortingArrays = new NativeArray<NativeArray<RenderData>>(outQueuesLength, Allocator.Temp);
            for (var i = 0; i < sortingArrays.Length; i++)
            {
                sortingArrays[i] = new NativeArray<RenderData>(sortingQueues[i].SortingQueue.Count, Allocator.TempJob);
            }

            for (var i = 0; i < outQueuesLength; i++)
            {
                if (sortingQueues[i].SortingQueue.Count > 0)
                {
                    jobs.Add(new NativeQueueToArrayJob
                    {
                        NativeQueue = sortingQueues[i].SortingQueue,
                        NativeArray = sortingArrays[i]
                    }.Schedule());
                }
            }

            JobHandle.CompleteAll(jobs.AsArray());
            jobs.Dispose();

            for (var i = 0; i < sortingQueues.Length; i++)
            {
                sortingQueues[i].SortingQueue.Dispose();
            }

            sortingQueues.Dispose();

            return sortingArrays;
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
            NativeArray<NativeArray<RenderData>> sortingArrays,
            out NativeArray<int> startIndexes, out NativeArray<int> endIndexes)
        {
            var sharedSortingArray = new NativeArray<RenderData>(visibleEntitiesTotal, Allocator.TempJob);
            startIndexes = new NativeArray<int>(sortingArrays.Length, Allocator.TempJob);
            endIndexes = new NativeArray<int>(sortingArrays.Length, Allocator.TempJob);
            var sharedIndex = 0;

            for (var i = 0; i < sortingArrays.Length; i++)
            {
                startIndexes[i] = i > 0 ? endIndexes[i - 1] : 0;
                endIndexes[i] = startIndexes[i] + sortingArrays[i].Length;
                for (var j = 0; j < sortingArrays[i].Length; j++)
                {
                    var renderData = sortingArrays[i][j];
                    sharedSortingArray[sharedIndex] = renderData;
                    sharedIndex++;
                }
            }

            for (var i = 0; i < sortingArrays.Length; i++)
            {
                sortingArrays[i].Dispose();
            }

            sortingArrays.Dispose();

            return sharedSortingArray;
        }

        private static JobHandle ScheduleBubbleSortingOfEachSection(NativeArray<RenderData> sharedSortingArray,
            NativeArray<int> startIndexes, NativeArray<int> endIndexes)
        {
            var jobs = new NativeList<JobHandle>(startIndexes.Length, Allocator.Temp);
            for (var i = 0; i < startIndexes.Length; i++)
            {
                if (endIndexes[i] > startIndexes[i])
                {
                    jobs.Add(new SortByPositionParallelJob
                    {
                        SharedNativeArray = sharedSortingArray,
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

        private static JobHandle ScheduleWritingToDataContainers(NativeArray<RenderData> finalArrayOfRenderData,
            NativeArray<Matrix4x4> spriteMatrixArray,
            NativeArray<Vector4> spriteUvArray, JobHandle dependency, int visibleEntitiesTotal)
        {
            var fillArraysJob = new FillArraysJob
            {
                SortedArray = finalArrayOfRenderData,
                MatrixArray = spriteMatrixArray,
                UvArray = spriteUvArray
            };
            dependency = fillArraysJob.Schedule(visibleEntitiesTotal, 10, dependency);
            finalArrayOfRenderData.Dispose(dependency);
            return dependency;
        }

        [BurstCompile]
        private partial struct CullJob : IJobEntity
        {
            [ReadOnly] public float XLeft; // Left most cull position
            [ReadOnly] public float XRight; // Right most cull position
            [ReadOnly] public float YTop; // Top most cull position
            [ReadOnly] public float YBottom; // Bottom most cull position

            public NativeQueue<InventoryRenderData> InventoryRenderDataQueue;

            [ReadOnly] public NativeArray<float> Pivots;
            [ReadOnly] public int PivotCount;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<QueueContainer> SortingQueues;

            public void Execute(in Entity entity, in LocalTransform localTransform, in WorldSpriteSheetState animationData,
                in InventoryState inventory)
            {
                var positionX = localTransform.Position.x;
                if (!(positionX > XLeft) || !(positionX < XRight))
                {
                    // Unit is not within horizontal view-bounds. No need to render.
                    return;
                }

                var positionY = localTransform.Position.y;
                if (!(positionY > YBottom) || !(positionY < YTop))
                {
                    // Unit is not within vertical view-bounds. No need to render.
                    return;
                }

                var renderData = new RenderData
                {
                    Entity = entity,
                    Position = localTransform.Position,
                    Matrix = animationData.Matrix,
                    Uv = animationData.Uv
                };

                QuickSortToQueues(Pivots, 0, PivotCount, SortingQueues, 0, renderData);

                if (inventory.CurrentItem != InventoryItem.None)
                {
                    InventoryRenderDataQueue.Enqueue(new InventoryRenderData
                    {
                        Entity = entity,
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

            [ReadOnly] public NativeArray<float> Pivots;
            [ReadOnly] public int PivotCount;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<QueueContainer> SortingQueues;

            public void Execute(in Entity entity, in DroppedItem droppedItem, in LocalTransform localTransform)
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
                    Entity = entity,
                    Position = position,
                    Matrix = Matrix4x4.TRS(renderPosition, quaternion.identity, Vector3.one),
                    Uv = new Vector4(columnScale, rowScale, column * columnScale, row * rowScale)
                };

                QuickSortToQueues(Pivots, 0, PivotCount, SortingQueues, 0, renderData);
            }
        }

        [BurstCompile]
        private partial struct CullJobOnGridEntities : IJobEntity
        {
            [ReadOnly] public NativeArray<float> Pivots;
            [ReadOnly] public int PivotCount;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<QueueContainer> SortingQueues;

            public void Execute(in Entity entity, in WorldSpriteSheetState worldSpriteSheetState, in LocalTransform localTransform)
            {
                var position = localTransform.Position;
                var renderData = new RenderData
                {
                    Entity = entity,
                    Position = position,
                    Matrix = worldSpriteSheetState.Matrix,
                    Uv = worldSpriteSheetState.Uv
                };

                QuickSortToQueues(Pivots, 0, PivotCount, SortingQueues, 0, renderData);
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
            [ReadOnly] public int PivotsStartIndex;
            [ReadOnly] public int PivotCount;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<QueueContainer> SortingQueues;

            [ReadOnly] public int OutputStartIndex;

            public void Execute()
            {
                while (InQueue.Count > 0)
                {
                    var renderData = InQueue.Dequeue();
                    QuickSortToQueues(Pivots, PivotsStartIndex, PivotCount, SortingQueues, OutputStartIndex, renderData);
                }
            }
        }

        private static void QuickSortToQueues(NativeArray<float> pivots,
            int pivotsStartIndex,
            int pivotCount,
            NativeArray<QueueContainer> sortingQueues,
            int outputStartIndex,
            RenderData renderData)
        {
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
}
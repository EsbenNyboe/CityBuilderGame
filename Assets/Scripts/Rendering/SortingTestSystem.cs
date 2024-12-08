using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rendering
{
    [DisableAutoCreation]
    public partial struct SortingTestSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SortingTest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var sortingTest = SystemAPI.GetSingleton<SortingTest>();

            var sectionCount = (int)math.pow(sortingTest.SectionsPerSplitJob, sortingTest.SplitJobCount);
            var pivotCount = sectionCount - 1;
            var queueCount = 1;
            Debug.Log("Sections: " + sectionCount);
            Debug.Log("Pivots: " + pivotCount);

            var jobIndex = 0;
            while (jobIndex < sortingTest.SplitJobCount)
            {
                var previousQueueCount = queueCount;
                var outputQueues = (int)math.pow(sortingTest.SectionsPerSplitJob, jobIndex + 1);
                queueCount += outputQueues;
                Debug.Log("Queue count: " + previousQueueCount + " + " + outputQueues + " = " + queueCount);
                jobIndex++;
            }

            // var jobBatchSizes = new NativeArray<int>(sortingTest.SplitJobCount, Allocator.Temp);
            // for (var i = 0; i < sortingTest.SplitJobCount; i++)
            // {
            //     jobBatchSizes[i] = (int)math.pow(sortingTest.SectionsPerSplitJob, i);
            // }
            //
            // for (var i = 0; i < jobBatchSizes.Length; i++)
            // {
            //     Debug.Log("Job batch size " + i + ": " + jobBatchSizes[i]);
            // }
            //
            // jobBatchSizes.Dispose();

            var batchSectionCounts = new NativeArray<int>(sortingTest.SplitJobCount, Allocator.Temp);
            for (var i = 0; i < batchSectionCounts.Length; i++)
            {
                batchSectionCounts[i] = (int)math.pow(sortingTest.SectionsPerSplitJob, i + 1);
                Debug.Log("Job section count " + i + ": " + batchSectionCounts[i]);
            }

            float yBottom = -1;
            float yTop = 1;
            var pivots = GetArrayOfPivots(pivotCount, batchSectionCounts, yBottom, yTop);

            batchSectionCounts.Dispose();
            pivots.Dispose();
        }

        private static NativeArray<float> GetArrayOfPivots(int pivotCount, NativeArray<int> batchSectionCounts, float yBottom, float yTop)
        {
            var pivots = new NativeArray<float>(pivotCount, Allocator.Temp);
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
                    Debug.Log("Pivot " + pivotIndex + ": " + pivots[pivotIndex]);
                    pivotIndex++;
                }
            }

            return pivots;
        }
    }
}
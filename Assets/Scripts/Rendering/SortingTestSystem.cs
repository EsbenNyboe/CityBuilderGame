using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rendering
{
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

            var jobBatchSizes = new NativeArray<int>(sortingTest.SplitJobCount, Allocator.Temp);
            for (var i = 0; i < sortingTest.SplitJobCount; i++)
            {
                jobBatchSizes[i] = (int)math.pow(sortingTest.SectionsPerSplitJob, i);
            }

            for (var i = 0; i < jobBatchSizes.Length; i++)
            {
                Debug.Log("Job batch size " + i + ": " + jobBatchSizes[i]);
            }

            jobBatchSizes.Dispose();
        }
    }
}
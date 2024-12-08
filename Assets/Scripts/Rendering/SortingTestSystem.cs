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
            state.RequireForUpdate<CameraInformation>();
            state.RequireForUpdate<SortingJobConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var sortingTest = SystemAPI.GetSingleton<SortingJobConfig>();

            if (!sortingTest.EnableDebugging)
            {
                return;
            }

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

            var batchSectionCounts = new NativeArray<int>(sortingTest.SplitJobCount, Allocator.Temp);
            for (var i = 0; i < batchSectionCounts.Length; i++)
            {
                batchSectionCounts[i] = (int)math.pow(sortingTest.SectionsPerSplitJob, i + 1);
                Debug.Log("Job section count " + i + ": " + batchSectionCounts[i]);
            }


            var cameraInformation = SystemAPI.GetSingleton<CameraInformation>();
            var cameraPosition = cameraInformation.CameraPosition;
            var screenRatio = cameraInformation.ScreenRatio;
            var orthographicSize = cameraInformation.OrthographicSize;

            var cullBuffer = 1f; // We add some buffer, so culling is not noticable
            var cameraSizeX = orthographicSize * screenRatio + cullBuffer;
            var cameraSizeY = orthographicSize + cullBuffer;

            var xLeft = cameraPosition.x - cameraSizeX;
            var xRight = cameraPosition.x + cameraSizeX;
            var yTop = cameraPosition.y + cameraSizeY;
            var yBottom = cameraPosition.y - cameraSizeY;

            var pivots = GetArrayOfPivots(pivotCount, batchSectionCounts, yBottom, yTop);
            for (var i = 0; i < pivots.Length; i++)
            {
                Debug.DrawLine(new Vector3(xLeft, pivots[i], 0), new Vector3(xRight, pivots[i], 0), Color.green);
            }

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
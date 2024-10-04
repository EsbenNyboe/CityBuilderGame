using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

public partial class ParallelJobTestSystem : SystemBase
{
    private readonly static int iterations = 10;
    private readonly static int innerIterations = 10;

    protected override void OnUpdate()
    {
        //// TEST A: Complete one job at a time
        //foreach (var item in SystemAPI.Query<RefRO<PathFollow>>())
        //{
        //    JobHandle jobHandle = new ExpensiveJob().Schedule();
        //    jobHandle.Complete();
        //}

        //// TEST B: Complete a list of jobs (IS PARALLEL)
        //NativeList<JobHandle> jobHandleList = new NativeList<JobHandle>(Allocator.Temp);

        //foreach (var item in SystemAPI.Query<RefRO<PathFollow>>())
        //{
        //    var expensiveJob = new ExpensiveJob();
        //    jobHandleList.Add(expensiveJob.Schedule());
        //}

        //JobHandle.CompleteAll(jobHandleList.AsArray());
        //jobHandleList.Dispose();

        //// TEST C: Complete a parallel job (IS PARALLEL, AND A BIT FASTER)
        //NativeList<int> parallelJobData = new NativeList<int>(Allocator.TempJob);

        //foreach (var item in SystemAPI.Query<RefRO<PathFollow>>())
        //{
        //    parallelJobData.Add(0);
        //}

        //var expensiveJobParallel = new ExpensiveJobParallel();
        //expensiveJobParallel.ParallelJobData = parallelJobData;
        //JobHandle jobHandle = expensiveJobParallel.Schedule(parallelJobData.Length, 1);
        //jobHandle.Complete();

        //parallelJobData.Dispose();
    }

    private struct ExpensiveJob : IJob
    {
        public void Execute()
        {
            ExpensiveOperation();
        }
    }

    private struct ExpensiveJobParallel : IJobParallelFor
    {
        [ReadOnly]
        public NativeList<int> ParallelJobData;

        public void Execute(int index)
        {
            ExpensiveOperation();
        }
    }

    private static void ExpensiveOperation()
    {
        // Loop through a fixed number of iterations, which will heavily impact performance.
        for (int i = 0; i < iterations; i++)
        {
            // Log progress every 100 iterations to show Unity is still responsive
            if (i % 100 == 0)
            {
                //Debug.Log("Iteration: " + i);
            }

            // This inner loop performs heavy math calculations
            for (int j = 0; j < innerIterations; j++)
            {
                // Perform an expensive and pointless calculation.
                // Using Mathf functions will result in expensive trigonometric calculations.
                float dummyValue = Mathf.Sin(j) * Mathf.Cos(i) * Mathf.Sqrt(i * j);

                // Just to make sure the compiler doesn't optimize away our calculations
                dummyValue += Mathf.Pow(dummyValue, 2) / Mathf.Tan(dummyValue);

                // Use Mathf.Log to add another expensive operation
                dummyValue = Mathf.Log(dummyValue + 1.0001f) + Mathf.Exp(dummyValue);

                // Do something with the result, even though it's not useful.
                // This is to prevent it from being optimized away by the compiler.
                if (dummyValue == float.MaxValue)
                {
                    Debug.LogError("Should never hit this.");
                }
            }
        }
    }
}

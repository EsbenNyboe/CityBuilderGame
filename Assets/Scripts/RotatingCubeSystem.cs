using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

public partial struct RotatingCubeSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<RotateSpeed>();
    }

    [ BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;
        return;

        foreach (var (localTransform, rotateSpeed)
                 in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotateSpeed>>().WithAll<RotatingCubeAuthoring>())
        {
            var power = 1f;
            for (var i = 0; i < 100000; i++)
            {
                power *= 2f;
                power /= 2f;
            }

            localTransform.ValueRW = localTransform.ValueRW.RotateY(rotateSpeed.ValueRO.Value * SystemAPI.Time.DeltaTime * power);
        }

        var rotatingCubeJob = new RotatingCubeJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        };
        rotatingCubeJob.ScheduleParallel();
    }


    [BurstCompile]
    [WithAll(typeof(RotatingCube))]
    public partial struct RotatingCubeJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref LocalTransform localTransform, in RotateSpeed rotateSpeed)
        {
            var power = 1f;
            for (var i = 0; i < 100000; i++)
            {
                power *= 2f;
                power /= 2f;
            }

            localTransform = localTransform.RotateY(rotateSpeed.Value * DeltaTime * power);
        }
    }
}
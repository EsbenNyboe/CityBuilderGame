using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

public struct MoodRestlessness : IComponentData
{
    public float Restlessness;
}

[UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
public partial struct MoodRestlessnessSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new UpdateRestlessnessJob { DeltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel();
    }

    [BurstCompile]
    private partial struct UpdateRestlessnessJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;

        public void Execute(in IsIdle _, in PathFollow pathFollow, ref MoodRestlessness moodLoneliness)
        {
            if (!pathFollow.IsMoving())
            {
                moodLoneliness.Restlessness += DeltaTime;
            }
        }
    }
}
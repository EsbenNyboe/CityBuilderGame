using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace UnitState.Mood
{
    internal partial struct MoodLonelinessSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new UpdateLonelinessJob { DeltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel();
        }

        [BurstCompile]
        private partial struct UpdateLonelinessJob : IJobEntity
        {
            [ReadOnly] public float DeltaTime;

            [UsedImplicitly]
            public void Execute(ref MoodLoneliness moodLoneliness)
            {
                moodLoneliness.Loneliness += DeltaTime;
            }
        }
    }
}
using CustomTimeCore;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace UnitState.Mood
{
    internal partial struct MoodLonelinessSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            new UpdateLonelinessJob { DeltaTime = SystemAPI.Time.DeltaTime * timeScale }.ScheduleParallel();
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
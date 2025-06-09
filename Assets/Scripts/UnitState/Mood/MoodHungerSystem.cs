using CustomTimeCore;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace UnitState.Mood
{
    internal partial struct MoodHungerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            new UpdateHungerJob { DeltaTime = SystemAPI.Time.DeltaTime * timeScale }.ScheduleParallel();
        }

        [BurstCompile]
        private partial struct UpdateHungerJob : IJobEntity
        {
            [ReadOnly] public float DeltaTime;

            [UsedImplicitly]
            public void Execute(ref MoodHunger moodHunger)
            {
                moodHunger.Hunger += DeltaTime;
            }
        }
    }
}
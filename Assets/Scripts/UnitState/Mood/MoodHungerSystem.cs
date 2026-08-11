using CustomTimeCore;
using JetBrains.Annotations;
using UnitBehaviours.UnitManagers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace UnitState.Mood
{
    internal partial struct MoodHungerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<CustomTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var unitBehaviourManager = SystemAPI.GetSingleton<UnitBehaviourManager>();
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            var hungerPerFrame = unitBehaviourManager.HungerPerSecWhenIdle * SystemAPI.Time.DeltaTime * timeScale;
            new UpdateHungerJob { HungerPerFrame = hungerPerFrame }.ScheduleParallel();
        }

        [BurstCompile]
        private partial struct UpdateHungerJob : IJobEntity
        {
            [ReadOnly] public float HungerPerFrame;

            [UsedImplicitly]
            public void Execute(ref MoodHunger moodHunger)
            {
                moodHunger.Hunger += HungerPerFrame;
            }
        }
    }
}
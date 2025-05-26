using CustomTimeCore;
using Unity.Burst;
using Unity.Entities;

namespace UnitState.Mood
{
    public partial struct MoodInitiativeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            const float initiativeRegenerationFactor = 1f;

            foreach (var moodInitiative in SystemAPI.Query<RefRW<MoodInitiative>>())
            {
                if (moodInitiative.ValueRO.Initiative < 1f)
                {
                    moodInitiative.ValueRW.Initiative += initiativeRegenerationFactor * SystemAPI.Time.DeltaTime * timeScale;
                }
            }
        }
    }
}
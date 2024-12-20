using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace UnitState.Mood
{
    public partial struct MoodInitiativeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            const float initiativeRegenerationFactor = 1f;

            foreach (var moodInitiative in SystemAPI.Query<RefRW<MoodInitiative>>())
            {
                if (moodInitiative.ValueRO.Initiative < 1f)
                {
                    moodInitiative.ValueRW.Initiative += initiativeRegenerationFactor * Time.deltaTime;
                }
            }
        }
    }
}
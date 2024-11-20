using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace UnitState
{
    public partial struct MoodInitiative : IComponentData
    {
        public float Initiative;
    }

    public partial struct MoodInitiative
    {
        public readonly bool HasInitiative()
        {
            return Initiative >= 1f;
        }

        public void UseInitiative()
        {
            Initiative = 0;
        }
    }

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
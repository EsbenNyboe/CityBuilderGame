using Unity.Entities;

namespace UnitState.Mood
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
}
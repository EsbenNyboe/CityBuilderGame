using UnitControl;
using UnitState.Mood;
using Unity.Entities;

namespace Statistics
{
    public partial class SelectedUnitStatsManagerSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var selectedUnitCount = GetEntityQuery(typeof(UnitSelection)).CalculateEntityCount();
            SelectedUnitStats.Instance.SetActive(selectedUnitCount > 0);

            foreach (var (_, moodSleepiness, moodLoneliness) in SystemAPI
                         .Query<RefRO<UnitSelection>, RefRO<MoodSleepiness>, RefRO<MoodLoneliness>>())
            {
                SelectedUnitStats.Instance.Sleepiness.AddValue(moodSleepiness.ValueRO.Sleepiness);
                SelectedUnitStats.Instance.Loneliness.AddValue(moodLoneliness.ValueRO.Loneliness);
            }
        }
    }
}
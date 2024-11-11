using Unity.Entities;
using UnityEngine;

public struct MoodSleepiness : IComponentData
{
    public float Sleepiness;
    public double MostRecentSleepAction;
}

public class MoodSleepinessAuthoring : MonoBehaviour
{
    [SerializeField] private float _sleepiness;

    public class MoodSleepinessBaker : Baker<MoodSleepinessAuthoring>
    {
        public override void Bake(MoodSleepinessAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MoodSleepiness { Sleepiness = authoring._sleepiness });
        }
    }
}
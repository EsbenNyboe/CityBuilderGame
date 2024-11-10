using Unity.Entities;
using UnityEngine;

public struct MoodRestlessness : IComponentData
{
    public float TimeSpentDoingNothing;
}

public class MoodRestlessnessAuthoring : MonoBehaviour
{
    [SerializeField] private float _timeSpentDoingNothing;

    public class MoodRestlessnessBaker : Baker<MoodRestlessnessAuthoring>
    {
        public override void Bake(MoodRestlessnessAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MoodRestlessness { TimeSpentDoingNothing = authoring._timeSpentDoingNothing });
        }
    }
}
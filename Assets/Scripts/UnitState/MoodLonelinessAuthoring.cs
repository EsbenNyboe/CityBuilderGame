using Unity.Entities;
using UnityEngine;

public struct MoodLoneliness : IComponentData
{
    public float Value;
}

public class MoodLonelinessAuthoring : MonoBehaviour
{
    public float MoodLoneliness;

    public class MoodLonelinessBaker : Baker<MoodLonelinessAuthoring>
    {
        public override void Bake(MoodLonelinessAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new MoodLoneliness { Value = authoring.MoodLoneliness });
        }
    }
}
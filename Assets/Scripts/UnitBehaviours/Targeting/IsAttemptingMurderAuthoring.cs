using Unity.Entities;
using UnityEngine;

namespace UnitBehaviours.Pathing
{
    public struct IsAttemptingMurder : IComponentData
    {
    }

    public class IsAttemptingMurderAuthoring : MonoBehaviour
    {
        public class IsAttemptingMurderBaker : Baker<IsAttemptingMurderAuthoring>
        {
            public override void Bake(IsAttemptingMurderAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<IsAttemptingMurder>(entity);
            }
        }
    }
}
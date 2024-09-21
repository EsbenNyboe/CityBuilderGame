using Unity.Entities;
using UnityEngine;

public class UnitPatrolAuthoring : MonoBehaviour
{
    public class Baker : Baker<UnitPatrolAuthoring>
    {
        public override void Bake(UnitPatrolAuthoring authoring)
        {
            var entity = GetEntity(authoring);
            AddComponent(entity, new UnitPatrol());
        }
    }
}

public struct UnitPatrol : IComponentData
{
    public bool IsPatrolling;
}
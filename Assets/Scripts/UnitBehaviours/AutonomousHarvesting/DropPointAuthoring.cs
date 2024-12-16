using GridEntityNS;
using UnitBehaviours.Tags;
using UnitBehaviours.Targeting.Core;
using Unity.Entities;
using UnityEngine;

namespace UnitBehaviours.AutonomousHarvesting
{
    public class DropPointAuthoring : MonoBehaviour
    {
        public class Baker : Baker<DropPointAuthoring>
        {
            public override void Bake(DropPointAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<DropPoint>(entity);
                AddComponent<GridEntity>(entity);
                AddComponent<QuadrantEntity>(entity);
            }
        }
    }
}
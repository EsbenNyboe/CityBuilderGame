using GridEntityNS;
using UnitBehaviours.Targeting.Core;
using Unity.Entities;
using UnityEngine;

namespace UnitBehaviours.AutonomousHarvesting
{
    public class BerryBushAuthoring : MonoBehaviour
    {
        public class BerryBushBaker : Baker<BerryBushAuthoring>
        {
            public override void Bake(BerryBushAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<BerryBush>(entity);
                AddComponent<GridEntity>(entity);
                AddComponent<Damageable>(entity);
                AddComponent<QuadrantEntity>(entity);
            }
        }
    }

    public struct BerryBush : IComponentData
    {
    }
}
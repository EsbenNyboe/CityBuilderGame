using UnitBehaviours.Targeting;
using UnitState;
using Unity.Entities;
using UnityEngine;

namespace UnitBehaviours.Pathing
{
    public struct Zombie : IComponentData
    {
    }

    public class ZombieAuthoring : MonoBehaviour
    {
        public class ZombieBaker : Baker<ZombieAuthoring>
        {
            public override void Bake(ZombieAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<IsAlive>(entity);
                SetComponentEnabled<IsAlive>(entity, true);
                AddComponent<QuadrantEntity>(entity);

                AddComponent<Zombie>(entity);
                AddComponent<IsAttemptingMurder>(entity);
                AddComponent<TargetFollow>(entity);
                AddComponent<TargetSelector>(entity);
            }
        }
    }
}
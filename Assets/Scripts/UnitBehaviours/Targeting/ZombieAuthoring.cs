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
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<Zombie>(entity);
            }
        }
    }
}
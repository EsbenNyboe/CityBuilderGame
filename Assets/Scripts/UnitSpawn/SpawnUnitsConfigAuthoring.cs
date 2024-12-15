using Unity.Entities;
using UnityEngine;

namespace UnitSpawn
{
    public class SpawnUnitsConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject _objectToSpawn;

        [SerializeField] private int _amountToSpawn;

        public class Baker : Baker<SpawnUnitsConfigAuthoring>
        {
            public override void Bake(SpawnUnitsConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SpawnUnitsConfig
                {
                    ObjectToSpawn = GetEntity(authoring._objectToSpawn, TransformUsageFlags.Dynamic),
                    AmountToSpawn = authoring._amountToSpawn
                });
            }
        }
    }

    public struct SpawnUnitsConfig : IComponentData
    {
        public Entity ObjectToSpawn;
        public int AmountToSpawn;
    }
}
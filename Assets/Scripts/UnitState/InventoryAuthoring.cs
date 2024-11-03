using System;
using Unity.Entities;
using UnityEngine;

namespace UnitState
{
    [Serializable]
    public enum InventoryItem
    {
        None,
        LogOfWood
    }

    public struct Inventory : IComponentData
    {
        public InventoryItem CurrentItem;
    }

    public class InventoryAuthoring : MonoBehaviour
    {
        public InventoryItem CurrentItem;

        public class InventoryBaker : Baker<InventoryAuthoring>
        {
            public override void Bake(InventoryAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Inventory { CurrentItem = authoring.CurrentItem });
            }
        }
    }
}

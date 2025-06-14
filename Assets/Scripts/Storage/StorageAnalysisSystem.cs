using Grid;
using Inventory;
using UnitBehaviours.AutonomousHarvesting;
using Unity.Entities;
using Unity.Transforms;

namespace StorageNS
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(StorageRequestSystem))]
    public partial struct StorageAnalysisSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gridManager = SystemAPI.GetSingleton<GridManager>();

            foreach (var (storage, localTransform) in SystemAPI
                         .Query<DynamicBuffer<Storage>, RefRO<LocalTransform>>())
            {
                var storageCountLog = 0;
                var storageCountRawMeat = 0;
                var storageCountCookedMeat = 0;
                for (var i = 0; i < storage.Length; i++)
                {
                    if (storage[i].Item == InventoryItem.None)
                    {
                        continue;
                    }

                    if (storage[i].Item == InventoryItem.LogOfWood)
                    {
                        storageCountLog++;
                    }
                    else if (storage[i].Item == InventoryItem.RawMeat)
                    {
                        storageCountRawMeat++;
                    }
                    else if (storage[i].Item == InventoryItem.CookedMeat)
                    {
                        storageCountCookedMeat++;
                    }
                }

                var cell = GridHelpers.GetXY(localTransform.ValueRO.Position);
                gridManager.SetStorageCount(cell, storageCountLog, InventoryItem.LogOfWood);
                gridManager.SetStorageCount(cell, storageCountRawMeat, InventoryItem.RawMeat);
                gridManager.SetStorageCount(cell, storageCountCookedMeat, InventoryItem.CookedMeat);
            }

            SystemAPI.SetSingleton(gridManager);
        }
    }
}
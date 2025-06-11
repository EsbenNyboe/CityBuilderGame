using Grid;
using Inventory;
using Unity.Entities;
using Unity.Transforms;

namespace Storage
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
                         .Query<DynamicBuffer<UnitBehaviours.AutonomousHarvesting.Storage>, RefRO<LocalTransform>>())
            {
                var storageCount = 0;
                for (var i = 0; i < storage.Length; i++)
                {
                    if (storage[i].Item != InventoryItem.None)
                    {
                        storageCount++;
                    }
                }

                var cell = GridHelpers.GetXY(localTransform.ValueRO.Position);
                gridManager.SetStorageCount(cell, storageCount);
            }

            SystemAPI.SetSingleton(gridManager);
        }
    }
}
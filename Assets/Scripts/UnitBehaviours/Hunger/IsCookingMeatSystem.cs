using CustomTimeCore;
using Grid;
using Inventory;
using SpriteTransformNS;
using SystemGroups;
using UnitAgency.Data;
using UnitBehaviours.Hunger;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using EndSimulationEntityCommandBufferSystem = Unity.Entities.EndSimulationEntityCommandBufferSystem;
using ISystem = Unity.Entities.ISystem;
using SystemAPI = Unity.Entities.SystemAPI;
using SystemState = Unity.Entities.SystemState;

namespace UnitBehaviours.CookingMeat
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsCookingMeatSystem : ISystem
    {
        private const float TimeToCookMeat = 3f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);


            foreach (var (isCookingMeat, inventory, spriteTransform, localTransform, entity) in SystemAPI
                         .Query<RefRW<IsCookingMeat>, RefRW<InventoryState>, RefRW<SpriteTransform>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                var cell = GridHelpers.GetXY(localTransform.ValueRO.Position);
                if (!gridManager.TryGetNeighbouringBonfireCell(cell, out var bonfireCell))
                {
                    // I'm not next to a Bonfire...
                    ecb.RemoveComponent<IsCookingMeat>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                var xDiff = bonfireCell.x - cell.x;
                var angleInDegrees = xDiff > 0 ? 0f : 180f;
                spriteTransform.ValueRW.Rotation = quaternion.EulerZXY(0, math.PI / 180 * angleInDegrees, 0);

                if (isCookingMeat.ValueRO.CookingProgress > TimeToCookMeat)
                {
                    isCookingMeat.ValueRW.CookingProgress = 0;
                    // I finished cooking my meat!
                    ecb.RemoveComponent<IsCookingMeat>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    inventory.ValueRW.CurrentItem = InventoryItem.CookedMeat;
                    continue;
                }

                isCookingMeat.ValueRW.CookingProgress += SystemAPI.Time.DeltaTime * timeScale;
            }
        }
    }
}
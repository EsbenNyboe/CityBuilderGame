using CustomTimeCore;
using Debugging;
using Grid;
using Inventory;
using SystemGroups;
using UnitAgency.Data;
using UnitBehaviours.Pathing;
using UnitState.Mood;
using Unity.Burst;
using Unity.Entities;

namespace UnitBehaviours.Hunger
{
    [UpdateInGroup(typeof(UnitBehaviourGridWritingSystemGroup))]
    public partial struct IsEatingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<DebugToggleManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var hungerPerSecWhenEating = -0.2f * SystemAPI.Time.DeltaTime * timeScale;
            var foodPerSecWhenEating = -0.2f * SystemAPI.Time.DeltaTime * timeScale;
            var gridManager = SystemAPI.GetSingleton<GridManager>();

            foreach (var (isEating,
                         inventory,
                         pathFollow,
                         moodHunger,
                         entity) in SystemAPI
                         .Query<RefRW<IsEating>, RefRW<InventoryState>, RefRO<PathFollow>, RefRW<MoodHunger>>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    ecb.RemoveComponent<IsEating>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                if (inventory.ValueRO.CurrentDurability > 0)
                {
                    moodHunger.ValueRW.Hunger += hungerPerSecWhenEating;
                    inventory.ValueRW.CurrentDurability += foodPerSecWhenEating;
                    inventory.ValueRW.CurrentItem = InventoryItem.None;
                }
                else
                {
                    ecb.RemoveComponent<IsEating>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    inventory.ValueRW.CurrentItem = InventoryItem.None;
                }
            }

            SystemAPI.SetSingleton(gridManager);
        }
    }
}
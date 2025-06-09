using CustomTimeCore;
using Debugging;
using Grid;
using Inventory;
using SystemGroups;
using UnitAgency.Data;
using UnitBehaviours.Pathing;
using UnitBehaviours.UnitManagers;
using UnitState.Mood;
using Unity.Burst;
using Unity.Entities;

namespace UnitBehaviours.Hunger
{
    [UpdateInGroup(typeof(UnitBehaviourGridWritingSystemGroup))]
    public partial struct IsEatingMeatSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<CustomTime>();
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<DebugToggleManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

            var unitBehaviourManager = SystemAPI.GetSingleton<UnitBehaviourManager>();
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var foodPerSecWhenEating = -0.2f * SystemAPI.Time.DeltaTime * timeScale;
            var gridManager = SystemAPI.GetSingleton<GridManager>();

            foreach (var (isEatingMeat,
                         inventory,
                         pathFollow,
                         moodHunger,
                         entity) in SystemAPI
                         .Query<RefRW<IsEatingMeat>, RefRW<InventoryState>, RefRO<PathFollow>, RefRW<MoodHunger>>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    ecb.RemoveComponent<IsEatingMeat>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                if (inventory.ValueRO.CurrentDurability > 0)
                {
                    moodHunger.ValueRW.Hunger += unitBehaviourManager.HungerPerSec * SystemAPI.Time.DeltaTime * timeScale;
                    inventory.ValueRW.CurrentDurability += unitBehaviourManager.DurabilityPerSec * SystemAPI.Time.DeltaTime * timeScale;
                }
                else
                {
                    ecb.RemoveComponent<IsEatingMeat>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    inventory.ValueRW.CurrentItem = InventoryItem.None;
                }
            }

            SystemAPI.SetSingleton(gridManager);
        }
    }
}
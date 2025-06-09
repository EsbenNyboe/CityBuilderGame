using Inventory;
using Rendering;
using SystemGroups;
using UnitBehaviours.Hunger;
using UnitBehaviours.Pathing;
using UnitBehaviours.Sleeping;
using UnitBehaviours.Talking;
using UnitBehaviours.Targeting;
using Unity.Burst;
using Unity.Entities;

namespace UnitBehaviours.UnitConfigurators
{
    [UpdateInGroup(typeof(AnimationSystemGroup))]
    [UpdateBefore(typeof(WorldSpriteSheetAnimationSystem))]
    public partial struct UnitAnimationSelectionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Boar
            foreach (var (unitAnimationSelection, pathFollow) in SystemAPI.Query<RefRW<UnitAnimationSelection>, RefRO<PathFollow>>().WithAll<Boar>())
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = pathFollow.ValueRO.IsMoving()
                    ? WorldSpriteSheetEntryType.BoarRun
                    : WorldSpriteSheetEntryType.BoarStand;
            }

            // Baby Villager
            foreach (var (unitAnimationSelection, pathFollow, inventory) in SystemAPI
                         .Query<RefRW<UnitAnimationSelection>, RefRO<PathFollow>, RefRO<InventoryState>>()
                         .WithNone<IsSleeping>().WithNone<IsTalking>().WithNone<IsThrowingSpear>().WithAll<Villager>().WithAll<Baby>())
            {
                var hasItem = inventory.ValueRO.CurrentItem != InventoryItem.None;
                if (pathFollow.ValueRO.IsMoving())
                {
                    unitAnimationSelection.ValueRW.SelectedAnimation =
                        hasItem ? WorldSpriteSheetEntryType.WalkHolding : WorldSpriteSheetEntryType.BabyWalk;
                }
                else
                {
                    unitAnimationSelection.ValueRW.SelectedAnimation =
                        hasItem ? WorldSpriteSheetEntryType.IdleHolding : WorldSpriteSheetEntryType.BabyIdle;
                }
            }

            foreach (var unitAnimationSelection in SystemAPI.Query<RefRW<UnitAnimationSelection>>()
                         .WithPresent<IsSleeping>().WithAll<Baby>())
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = WorldSpriteSheetEntryType.BabySleep;
            }

            foreach (var unitAnimationSelection in SystemAPI.Query<RefRW<UnitAnimationSelection>>().WithAll<IsTalking>().WithAll<Baby>())
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = WorldSpriteSheetEntryType.BabyWalk;
            }

            // Villager
            foreach (var (unitAnimationSelection, pathFollow, inventory) in SystemAPI
                         .Query<RefRW<UnitAnimationSelection>, RefRO<PathFollow>, RefRO<InventoryState>>()
                         .WithNone<IsSleeping>().WithNone<IsTalking>().WithNone<IsThrowingSpear>().WithAll<Villager>().WithNone<Baby>())
            {
                // THIS WILL BE OVERWRITTEN BY PARTICULAR BEHAVIOURS
                var hasItem = inventory.ValueRO.CurrentItem != InventoryItem.None;
                if (pathFollow.ValueRO.IsMoving())
                {
                    unitAnimationSelection.ValueRW.SelectedAnimation =
                        hasItem ? WorldSpriteSheetEntryType.WalkHolding : WorldSpriteSheetEntryType.Walk;
                }
                else
                {
                    unitAnimationSelection.ValueRW.SelectedAnimation =
                        hasItem ? WorldSpriteSheetEntryType.IdleHolding : WorldSpriteSheetEntryType.Idle;
                }
            }

            foreach (var (unitAnimationSelection, isCookingMeat) in SystemAPI.Query<RefRW<UnitAnimationSelection>, RefRO<IsCookingMeat>>())
            {
                // TODO: Clean up this logic
                var cookingProgress = isCookingMeat.ValueRO.CookingProgress;
                unitAnimationSelection.ValueRW.SelectedAnimation = cookingProgress < 2f
                    ? WorldSpriteSheetEntryType.VillagerCookMeat
                    : WorldSpriteSheetEntryType.VillagerCookMeatDone;
            }

            foreach (var (unitAnimationSelection, isEmpty) in SystemAPI.Query<RefRW<UnitAnimationSelection>, RefRO<IsEating>>().WithNone<Baby>())
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = WorldSpriteSheetEntryType.VillagerEat;
            }

            foreach (var (unitAnimationSelection, isEmpty) in SystemAPI.Query<RefRW<UnitAnimationSelection>, RefRO<IsEating>>().WithAll<Baby>())
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = WorldSpriteSheetEntryType.BabyEat;
            }

            foreach (var unitAnimationSelection in SystemAPI.Query<RefRW<UnitAnimationSelection>>()
                         .WithPresent<IsSleeping>().WithNone<Baby>())
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = WorldSpriteSheetEntryType.Sleep;
            }

            foreach (var unitAnimationSelection in SystemAPI.Query<RefRW<UnitAnimationSelection>>().WithAll<IsTalking>().WithNone<Baby>())
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = WorldSpriteSheetEntryType.Talk;
            }

            foreach (var (unitAnimationSelection, _) in
                     SystemAPI.Query<RefRW<UnitAnimationSelection>, RefRO<IsThrowingSpear>>().WithAll<IsHoldingSpear>())
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = WorldSpriteSheetEntryType.SpearHolding;
            }

            foreach (var (unitAnimationSelection, _) in SystemAPI.Query<RefRW<UnitAnimationSelection>, RefRO<IsThrowingSpear>>()
                         .WithNone<IsHoldingSpear>())
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = WorldSpriteSheetEntryType.SpearThrowing;
            }
        }
    }
}
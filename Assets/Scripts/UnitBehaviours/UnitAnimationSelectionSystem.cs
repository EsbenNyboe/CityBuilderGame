using Inventory;
using Rendering;
using SystemGroups;
using UnitBehaviours.Pathing;
using UnitBehaviours.Sleeping;
using UnitBehaviours.Talking;
using UnitBehaviours.Targeting;
using Unity.Burst;
using Unity.Entities;

namespace UnitBehaviours
{
    [UpdateInGroup(typeof(AnimationSystemGroup))]
    [UpdateBefore(typeof(WorldSpriteSheetAnimationSystem))]
    public partial struct UnitAnimationSelectionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (unitAnimationSelection, pathFollow) in SystemAPI.Query<RefRW<UnitAnimationSelection>, RefRO<PathFollow>>().WithAll<Boar>())
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = pathFollow.ValueRO.IsMoving()
                    ? WorldSpriteSheetEntryType.BoarRun
                    : WorldSpriteSheetEntryType.BoarStand;
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

            foreach (var (unitAnimationSelection, pathFollow, inventory) in SystemAPI
                         .Query<RefRW<UnitAnimationSelection>, RefRO<PathFollow>, RefRO<InventoryState>>()
                         .WithNone<IsSleeping>().WithNone<IsTalking>().WithNone<IsThrowingSpear>().WithAll<Villager>())
            {
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

            foreach (var unitAnimationSelection in SystemAPI.Query<RefRW<UnitAnimationSelection>>()
                         .WithPresent<IsSleeping>())
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = WorldSpriteSheetEntryType.Sleep;
            }

            foreach (var unitAnimationSelection in SystemAPI.Query<RefRW<UnitAnimationSelection>>().WithAll<IsTalking>())
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = WorldSpriteSheetEntryType.Talk;
            }
        }
    }
}
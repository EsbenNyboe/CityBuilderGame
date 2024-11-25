using Rendering;
using UnitBehaviours.Talking;
using UnitState;
using Unity.Burst;
using Unity.Entities;

public struct UnitAnimationSelection : IComponentData
{
    public WorldSpriteSheetEntryType SelectedAnimation;
    public WorldSpriteSheetEntryType CurrentAnimation;
}

[UpdateInGroup(typeof(AnimationSystemGroup))]
[UpdateAfter(typeof(UnitAnimationManagerSystem))]
public partial struct UnitAnimationSelectionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (unitAnimationSelection, pathFollow, inventory) in SystemAPI
                     .Query<RefRW<UnitAnimationSelection>, RefRO<PathFollow>, RefRO<Inventory>>()
                     .WithNone<IsSleeping>().WithNone<IsTalking>())
        {
            var hasItem = inventory.ValueRO.CurrentItem != InventoryItem.None;
            if (pathFollow.ValueRO.PathIndex < 0)
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = hasItem ? WorldSpriteSheetEntryType.IdleHolding : WorldSpriteSheetEntryType.Idle;
            }
            else
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = hasItem ? WorldSpriteSheetEntryType.WalkHolding : WorldSpriteSheetEntryType.Walk;
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
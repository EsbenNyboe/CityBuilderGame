using Rendering;
using UnitBehaviours.Talking;
using UnitState;
using Unity.Burst;
using Unity.Entities;

public struct UnitAnimationSelection : IComponentData
{
    public AnimationId SelectedAnimation;
    public AnimationId CurrentAnimation;
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
                unitAnimationSelection.ValueRW.SelectedAnimation = hasItem ? AnimationId.IdleHolding : AnimationId.Idle;
            }
            else
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = hasItem ? AnimationId.WalkHolding : AnimationId.Walk;
            }
        }

        foreach (var unitAnimationSelection in SystemAPI.Query<RefRW<UnitAnimationSelection>>()
                     .WithPresent<IsSleeping>())
        {
            unitAnimationSelection.ValueRW.SelectedAnimation = AnimationId.Sleep;
        }

        foreach (var unitAnimationSelection in SystemAPI.Query<RefRW<UnitAnimationSelection>>().WithAll<IsTalking>())
        {
            unitAnimationSelection.ValueRW.SelectedAnimation = AnimationId.Talk;
        }
    }
}
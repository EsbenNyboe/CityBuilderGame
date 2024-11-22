using UnitBehaviours.Talking;
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
        foreach (var (unitAnimationSelection, pathFollow) in SystemAPI
                     .Query<RefRW<UnitAnimationSelection>, RefRO<PathFollow>>()
                     .WithNone<IsSleeping>().WithNone<IsTalking>())
        {
            if (pathFollow.ValueRO.PathIndex < 0)
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = AnimationId.Idle;
            }
            else
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = AnimationId.Walk;
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
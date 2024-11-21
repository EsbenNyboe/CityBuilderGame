using UnitBehaviours.Talking;
using Unity.Burst;
using Unity.Entities;

public struct UnitAnimationSelection : IComponentData
{
    public int SelectedAnimation;
    public int CurrentAnimation;
}

[UpdateInGroup(typeof(AnimationSystemGroup))]
[UpdateAfter(typeof(UnitAnimationManagerSystem))]
public partial struct UnitAnimationSelectionSystem : ISystem
{
    private SystemHandle _unitAnimationManagerSystem;

    public void OnCreate(ref SystemState state)
    {
        _unitAnimationManagerSystem = state.World.GetExistingSystem(typeof(UnitAnimationManagerSystem));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var unitAnimationManager = SystemAPI.GetComponent<UnitAnimationManager>(_unitAnimationManagerSystem);
        var talkAnimation = unitAnimationManager.TalkAnimation.SpriteRow;
        var sleepAnimation = unitAnimationManager.SleepAnimation.SpriteRow;
        var walkAnimation = unitAnimationManager.WalkAnimation.SpriteRow;
        var idleAnimation = unitAnimationManager.IdleAnimation.SpriteRow;

        foreach (var (unitAnimationSelection, pathFollow) in SystemAPI
                     .Query<RefRW<UnitAnimationSelection>, RefRO<PathFollow>>()
                     .WithNone<IsSleeping>().WithNone<IsTalking>())
        {
            if (pathFollow.ValueRO.PathIndex < 0)
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = idleAnimation;
            }
            else
            {
                unitAnimationSelection.ValueRW.SelectedAnimation = walkAnimation;
            }
        }

        foreach (var unitAnimationSelection in SystemAPI.Query<RefRW<UnitAnimationSelection>>()
                     .WithPresent<IsSleeping>())
        {
            unitAnimationSelection.ValueRW.SelectedAnimation = sleepAnimation;
        }

        foreach (var unitAnimationSelection in SystemAPI.Query<RefRW<UnitAnimationSelection>>().WithAll<IsTalking>())
        {
            unitAnimationSelection.ValueRW.SelectedAnimation = talkAnimation;
        }
    }
}
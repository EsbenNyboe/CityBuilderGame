using Unity.Burst;
using Unity.Entities;

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
        var sleepAnimation = unitAnimationManager.SleepAnimation.SpriteRow;
        var walkAnimation = unitAnimationManager.WalkAnimation.SpriteRow;
        var idleAnimation = unitAnimationManager.IdleAnimation.SpriteRow;

        foreach (var (unitAnimator, pathFollow) in SystemAPI.Query<RefRW<UnitAnimationSelection>, RefRO<PathFollow>>().WithNone<IsSleeping>())
        {
            if (pathFollow.ValueRO.PathIndex < 0)
            {
                unitAnimator.ValueRW.SelectedAnimation = idleAnimation;
            }
            else
            {
                unitAnimator.ValueRW.SelectedAnimation = walkAnimation;
            }
        }

        foreach (var unitAnimator in SystemAPI.Query<RefRW<UnitAnimationSelection>>().WithPresent<IsSleeping>())
        {
            unitAnimator.ValueRW.SelectedAnimation = sleepAnimation;
        }
    }
}
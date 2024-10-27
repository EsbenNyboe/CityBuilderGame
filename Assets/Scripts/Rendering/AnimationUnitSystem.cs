using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[UpdateAfter(typeof(UnitAnimationManagerSystem))]
public partial struct AnimationUnitSystem : ISystem
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
        var spriteRows = unitAnimationManager.SpriteRows;
        var scaleX = 1f / unitAnimationManager.SpriteColumns;
        var scaleY = 1f / spriteRows;
        var idleAnimation = unitAnimationManager.IdleAnimation;
        var walkAnimation = unitAnimationManager.WalkAnimation;
        var sleepAnimation = unitAnimationManager.SleepAnimation;

        foreach (var (spriteSheetAnimation, animationUnitIdle, pathFollow, entity) in SystemAPI
                     .Query<RefRW<SpriteSheetAnimation>, RefRO<AnimationUnitIdle>, RefRO<PathFollow>>()
                     .WithDisabled<AnimationUnitIdle>()
                     .WithNone<IsSleeping>()
                     .WithEntityAccess())
        {
            if (pathFollow.ValueRO.PathIndex < 0)
            {
                state.EntityManager.SetComponentEnabled<AnimationUnitWalk>(entity, false);
                state.EntityManager.SetComponentEnabled<AnimationUnitSleep>(entity, false);

                state.EntityManager.SetComponentEnabled<AnimationUnitIdle>(entity, true);
                SelectAnimation(spriteSheetAnimation, idleAnimation, spriteRows, scaleX, scaleY);
            }
        }

        foreach (var (spriteSheetAnimation, animationUnitWalk, pathFollow, entity) in SystemAPI
                     .Query<RefRW<SpriteSheetAnimation>, RefRO<AnimationUnitWalk>, RefRO<PathFollow>>()
                     .WithDisabled<AnimationUnitWalk>()
                     .WithNone<IsSleeping>()
                     .WithEntityAccess())
        {
            if (pathFollow.ValueRO.PathIndex >= 0)
            {
                state.EntityManager.SetComponentEnabled<AnimationUnitIdle>(entity, false);
                state.EntityManager.SetComponentEnabled<AnimationUnitSleep>(entity, false);

                state.EntityManager.SetComponentEnabled<AnimationUnitWalk>(entity, true);

                SelectAnimation(spriteSheetAnimation, walkAnimation, spriteRows, scaleX, scaleY);
            }
        }

        foreach (var (spriteSheetAnimation, animationUnitSleep, pathFollow, entity) in SystemAPI
                     .Query<RefRW<SpriteSheetAnimation>, RefRO<AnimationUnitSleep>, RefRO<PathFollow>>()
                     .WithDisabled<AnimationUnitSleep>()
                     .WithPresent<IsSleeping>()
                     .WithEntityAccess())
        {
            state.EntityManager.SetComponentEnabled<AnimationUnitIdle>(entity, false);
            state.EntityManager.SetComponentEnabled<AnimationUnitWalk>(entity, false);

            state.EntityManager.SetComponentEnabled<AnimationUnitSleep>(entity, true);

            SelectAnimation(spriteSheetAnimation, sleepAnimation, spriteRows, scaleX, scaleY);
        }
    }

    private static void SelectAnimation(RefRW<SpriteSheetAnimation> spriteSheetAnimation, AnimationConfig idleAnimation, int spriteRows, float scaleX,
        float scaleY)
    {
        spriteSheetAnimation.ValueRW.FrameCount = idleAnimation.FrameCount;
        spriteSheetAnimation.ValueRW.FrameTimerMax = idleAnimation.FrameInterval;
        var offsetY = (float)idleAnimation.SpriteRow / spriteRows;
        spriteSheetAnimation.ValueRW.Uv = new Vector4(scaleX, scaleY, 0, offsetY);
    }
}
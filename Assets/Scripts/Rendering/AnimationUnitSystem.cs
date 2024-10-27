using Unity.Burst;
using Unity.Entities;
using UnityEngine;

public partial struct AnimationUnitSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);
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
                spriteSheetAnimation.ValueRW.FrameCount = animationUnitIdle.ValueRO.FrameCount;
                spriteSheetAnimation.ValueRW.FrameTimerMax = animationUnitIdle.ValueRO.FrameTimerMax;
                var scaleX = 1f / animationUnitIdle.ValueRO.SpriteColumns;
                var scaleY = 1f / animationUnitIdle.ValueRO.SpriteRows;
                var offsetY = (float)animationUnitIdle.ValueRO.FrameRow / animationUnitIdle.ValueRO.SpriteRows;
                spriteSheetAnimation.ValueRW.Uv = new Vector4(scaleX, scaleY, 0, offsetY);
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
                spriteSheetAnimation.ValueRW.FrameCount = animationUnitWalk.ValueRO.FrameCount;
                spriteSheetAnimation.ValueRW.FrameTimerMax = animationUnitWalk.ValueRO.FrameTimerMax;
                var scaleX = 1f / animationUnitWalk.ValueRO.SpriteColumns;
                var scaleY = 1f / animationUnitWalk.ValueRO.SpriteRows;
                var offsetY = (float)animationUnitWalk.ValueRO.FrameRow / animationUnitWalk.ValueRO.SpriteRows;
                spriteSheetAnimation.ValueRW.Uv = new Vector4(scaleX, scaleY, 0, offsetY);
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
            spriteSheetAnimation.ValueRW.FrameCount = animationUnitSleep.ValueRO.FrameCount;
            spriteSheetAnimation.ValueRW.FrameTimerMax = animationUnitSleep.ValueRO.FrameTimerMax;
            var scaleX = 1f / animationUnitSleep.ValueRO.SpriteColumns;
            var scaleY = 1f / animationUnitSleep.ValueRO.SpriteRows;
            var offsetY = (float)animationUnitSleep.ValueRO.FrameRow / animationUnitSleep.ValueRO.SpriteRows;
            spriteSheetAnimation.ValueRW.Uv = new Vector4(scaleX, scaleY, 0, offsetY);
        }

        // entityCommandBuffer.Playback(EntityManager);
    }
}
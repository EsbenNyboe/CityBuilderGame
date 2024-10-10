using Unity.Entities;
using UnityEngine;

public partial class AnimationUnitSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);
        foreach (var (spriteSheetAnimation, animationUnitIdle, pathFollow, entity) in SystemAPI
                     .Query<RefRW<SpriteSheetAnimation>, RefRO<AnimationUnitIdle>, RefRO<PathFollow>>().WithDisabled<AnimationUnitIdle>()
                     .WithEntityAccess())
        {
            if (pathFollow.ValueRO.PathIndex < 0)
            {
                EntityManager.SetComponentEnabled<AnimationUnitWalk>(entity, false);

                EntityManager.SetComponentEnabled<AnimationUnitIdle>(entity, true);
                spriteSheetAnimation.ValueRW.FrameCount = animationUnitIdle.ValueRO.FrameCount;
                spriteSheetAnimation.ValueRW.FrameTimerMax = animationUnitIdle.ValueRO.FrameTimerMax;
                spriteSheetAnimation.ValueRW.Uv = new Vector4(1f / animationUnitIdle.ValueRO.FrameCount, 0.5f, 0, animationUnitIdle.ValueRO.FrameRow);
            }
        }

        foreach (var (spriteSheetAnimation, animationUnitWalk, pathFollow, entity) in SystemAPI
                     .Query<RefRW<SpriteSheetAnimation>, RefRO<AnimationUnitWalk>, RefRO<PathFollow>>().WithDisabled<AnimationUnitWalk>()
                     .WithEntityAccess())
        {
            if (pathFollow.ValueRO.PathIndex >= 0)
            {
                EntityManager.SetComponentEnabled<AnimationUnitIdle>(entity, false);

                EntityManager.SetComponentEnabled<AnimationUnitWalk>(entity, true);
                spriteSheetAnimation.ValueRW.FrameCount = animationUnitWalk.ValueRO.FrameCount;
                spriteSheetAnimation.ValueRW.FrameTimerMax = animationUnitWalk.ValueRO.FrameTimerMax;
                spriteSheetAnimation.ValueRW.Uv = new Vector4(1f / animationUnitWalk.ValueRO.FrameCount, 0.5f, 0, animationUnitWalk.ValueRO.FrameRow);
            }
        }
        // entityCommandBuffer.Playback(EntityManager);
    }
}
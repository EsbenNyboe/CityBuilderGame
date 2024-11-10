using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(AnimationSystemGroup))]
[UpdateAfter(typeof(UnitAnimationSelectionSystem))]
public partial struct SpriteSheetAnimationSystem : ISystem
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

        var uvScaleX = 1f / unitAnimationManager.SpriteColumns;
        var uvScaleY = 1f / unitAnimationManager.SpriteRows;
        var uvTemplate = new Vector4(uvScaleX, uvScaleY, 0, 0);

        var talkAnimation = unitAnimationManager.TalkAnimation;
        var sleepAnimation = unitAnimationManager.SleepAnimation;
        var walkAnimation = unitAnimationManager.WalkAnimation;
        var idleAnimation = unitAnimationManager.IdleAnimation;


        foreach (var (spriteSheetAnimationData, localToWorld, spriteTransform, unitAnimator) in SystemAPI
                     .Query<RefRW<SpriteSheetAnimation>, RefRO<LocalToWorld>, RefRO<SpriteTransform>,
                         RefRW<UnitAnimationSelection>>())
        {
            var selectedAnimation = unitAnimator.ValueRO.SelectedAnimation;
            var animationConfig = selectedAnimation switch
            {
                0 => talkAnimation,
                1 => sleepAnimation,
                2 => walkAnimation,
                3 => idleAnimation,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (unitAnimator.ValueRO.CurrentAnimation != selectedAnimation)
            {
                unitAnimator.ValueRW.CurrentAnimation = selectedAnimation;
                SetMatrix(spriteSheetAnimationData, localToWorld, spriteTransform);
                SetUv(spriteSheetAnimationData, animationConfig, uvTemplate);
            }
            else
            {
                UpdateAnimation(ref state, spriteSheetAnimationData, animationConfig, out var updateUv);
                SetMatrix(spriteSheetAnimationData, localToWorld, spriteTransform);
                if (updateUv)
                {
                    SetUv(spriteSheetAnimationData, animationConfig, uvTemplate);
                }
            }
        }
    }

    private void UpdateAnimation(ref SystemState state, RefRW<SpriteSheetAnimation> spriteSheetAnimationData,
        AnimationConfig animationConfig,
        out bool updateUv)
    {
        updateUv = false;
        spriteSheetAnimationData.ValueRW.FrameTimer += SystemAPI.Time.DeltaTime;
        while (spriteSheetAnimationData.ValueRO.FrameTimer > animationConfig.FrameInterval)
        {
            spriteSheetAnimationData.ValueRW.FrameTimer -= animationConfig.FrameInterval;
            spriteSheetAnimationData.ValueRW.CurrentFrame =
                (spriteSheetAnimationData.ValueRO.CurrentFrame + 1) % animationConfig.FrameCount;
            updateUv = true;
        }
    }

    private static void SetMatrix(RefRW<SpriteSheetAnimation> spriteSheetAnimationData,
        RefRO<LocalToWorld> localToWorld,
        RefRO<SpriteTransform> spriteTransform)
    {
        var position = localToWorld.ValueRO.Position + spriteTransform.ValueRO.Position;
        var rotation = spriteTransform.ValueRO.Rotation;

        spriteSheetAnimationData.ValueRW.Matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
    }

    private static void SetUv(RefRW<SpriteSheetAnimation> spriteSheetAnimationData, AnimationConfig animationConfig,
        Vector4 uv)
    {
        var uvScaleX = uv.x;
        var uvOffsetX = uvScaleX * spriteSheetAnimationData.ValueRO.CurrentFrame;
        var uvScaleY = uv.y;
        var uvOffsetY = uvScaleY * animationConfig.SpriteRow;

        uv.z = uvOffsetX;
        uv.w = uvOffsetY;
        spriteSheetAnimationData.ValueRW.Uv = uv;
    }
}
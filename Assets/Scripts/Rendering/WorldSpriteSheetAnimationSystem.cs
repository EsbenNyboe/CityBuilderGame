using Rendering;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public struct WorldSpriteSheetAnimation : IComponentData
{
    public int CurrentFrame;
    public float FrameTimer;
    public Vector4 Uv;
    public Matrix4x4 Matrix;
}

[UpdateInGroup(typeof(AnimationSystemGroup))]
[UpdateAfter(typeof(UnitAnimationSelectionSystem))]
public partial struct WorldSpriteSheetAnimationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WorldSpriteSheetManager>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var worldSpriteSheetManager = SystemAPI.GetSingleton<WorldSpriteSheetManager>();

        var uvScaleX = worldSpriteSheetManager.ColumnScale;
        var uvScaleY = worldSpriteSheetManager.RowScale;
        var uvTemplate = new Vector4(uvScaleX, uvScaleY, 0, 0);

        foreach (var (spriteSheetAnimationData, localToWorld, spriteTransform, unitAnimator) in SystemAPI
                     .Query<RefRW<WorldSpriteSheetAnimation>, RefRO<LocalToWorld>, RefRO<SpriteTransform>,
                         RefRW<UnitAnimationSelection>>())
        {
            var selectedAnimation = unitAnimator.ValueRO.SelectedAnimation;
            var entry = worldSpriteSheetManager.Entries[(int)selectedAnimation];

            UpdateAnimation(ref state, spriteSheetAnimationData, entry, out var updateUv);
            if (unitAnimator.ValueRO.CurrentAnimation != selectedAnimation)
            {
                unitAnimator.ValueRW.CurrentAnimation = selectedAnimation;
                SetMatrix(spriteSheetAnimationData, localToWorld, spriteTransform);
                SetUv(spriteSheetAnimationData, entry, uvTemplate);
            }
            else
            {
                SetMatrix(spriteSheetAnimationData, localToWorld, spriteTransform);
                if (updateUv)
                {
                    SetUv(spriteSheetAnimationData, entry, uvTemplate);
                }
            }
        }
    }

    private void UpdateAnimation(ref SystemState state, RefRW<WorldSpriteSheetAnimation> spriteSheetAnimationData,
        WorldSpriteSheetEntry entry, out bool updateUv)
    {
        updateUv = false;
        spriteSheetAnimationData.ValueRW.FrameTimer += SystemAPI.Time.DeltaTime;
        while (spriteSheetAnimationData.ValueRO.FrameTimer > entry.FrameInterval)
        {
            spriteSheetAnimationData.ValueRW.FrameTimer -= entry.FrameInterval;
            spriteSheetAnimationData.ValueRW.CurrentFrame =
                (spriteSheetAnimationData.ValueRO.CurrentFrame + 1) % entry.EntryColumns.Length;
            updateUv = true;
        }
    }

    private static void SetMatrix(RefRW<WorldSpriteSheetAnimation> spriteSheetAnimationData,
        RefRO<LocalToWorld> localToWorld,
        RefRO<SpriteTransform> spriteTransform)
    {
        var position = localToWorld.ValueRO.Position + spriteTransform.ValueRO.Position;
        var rotation = spriteTransform.ValueRO.Rotation;

        spriteSheetAnimationData.ValueRW.Matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
    }

    private static void SetUv(RefRW<WorldSpriteSheetAnimation> spriteSheetAnimationData, WorldSpriteSheetEntry entry,
        Vector4 uv)
    {
        var currentFrame = spriteSheetAnimationData.ValueRO.CurrentFrame;
        uv.z = uv.x * entry.EntryColumns[currentFrame];
        uv.w = uv.y * entry.EntryRows[currentFrame];
        spriteSheetAnimationData.ValueRW.Uv = uv;
    }
}
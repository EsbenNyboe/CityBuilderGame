using Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

        new SetAnimationStateJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            UvTemplate = uvTemplate,
            WorldSpriteSheetEntries = worldSpriteSheetManager.Entries
        }.ScheduleParallel();
    }

    [BurstCompile]
    private partial struct SetAnimationStateJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public Vector4 UvTemplate;

        [ReadOnly] [NativeDisableContainerSafetyRestriction]
        public NativeArray<WorldSpriteSheetEntry> WorldSpriteSheetEntries;

        public void Execute(ref WorldSpriteSheetAnimation spriteSheetAnimationData, ref UnitAnimationSelection unitAnimationSelection,
            in LocalToWorld localToWorld, in SpriteTransform spriteTransform)
        {
            var selectedAnimation = unitAnimationSelection.SelectedAnimation;
            var entry = WorldSpriteSheetEntries[(int)selectedAnimation];

            if (unitAnimationSelection.CurrentAnimation != selectedAnimation)
            {
                unitAnimationSelection.CurrentAnimation = selectedAnimation;
                ResetAnimation(ref spriteSheetAnimationData);
                SetMatrix(ref spriteSheetAnimationData, localToWorld, spriteTransform);
                SetUv(ref spriteSheetAnimationData, entry, UvTemplate);
            }
            else
            {
                UpdateAnimation(DeltaTime, ref spriteSheetAnimationData, entry, out var updateUv);
                SetMatrix(ref spriteSheetAnimationData, localToWorld, spriteTransform);
                if (updateUv)
                {
                    SetUv(ref spriteSheetAnimationData, entry, UvTemplate);
                }
            }
        }

        private static void ResetAnimation(ref WorldSpriteSheetAnimation spriteSheetAnimationData)
        {
            spriteSheetAnimationData.FrameTimer = 0;
            spriteSheetAnimationData.CurrentFrame = 0;
        }

        private static void SetMatrix(ref WorldSpriteSheetAnimation spriteSheetAnimationData,
            LocalToWorld localToWorld,
            SpriteTransform spriteTransform)
        {
            var position = localToWorld.Position + spriteTransform.Position;
            var rotation = spriteTransform.Rotation;

            spriteSheetAnimationData.Matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
        }

        private static void UpdateAnimation(float deltaTime, ref WorldSpriteSheetAnimation spriteSheetAnimationData,
            WorldSpriteSheetEntry entry, out bool updateUv)
        {
            updateUv = false;
            spriteSheetAnimationData.FrameTimer += deltaTime;
            while (spriteSheetAnimationData.FrameTimer > entry.FrameInterval)
            {
                spriteSheetAnimationData.FrameTimer -= entry.FrameInterval;
                spriteSheetAnimationData.CurrentFrame =
                    (spriteSheetAnimationData.CurrentFrame + 1) % entry.EntryColumns.Length;
                updateUv = true;
            }
        }

        private static void SetUv(ref WorldSpriteSheetAnimation spriteSheetAnimationData, WorldSpriteSheetEntry entry, Vector4 uv)
        {
            var currentFrame = spriteSheetAnimationData.CurrentFrame;
            uv.z = uv.x * entry.EntryColumns[currentFrame];
            uv.w = uv.y * entry.EntryRows[currentFrame];
            spriteSheetAnimationData.Uv = new Vector4(uv.x, uv.y, uv.z, uv.w);
        }
    }
}
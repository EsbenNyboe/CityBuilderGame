using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(AnimationUnitSystem))]
public partial struct SpriteSheetAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (spriteSheetAnimationData, localToWorld, spriteTransform) in SystemAPI
                     .Query<RefRW<SpriteSheetAnimation>, RefRO<LocalToWorld>, RefRO<SpriteTransform>>())
        {
            spriteSheetAnimationData.ValueRW.FrameTimer += SystemAPI.Time.DeltaTime;
            while (spriteSheetAnimationData.ValueRO.FrameTimer > spriteSheetAnimationData.ValueRO.FrameTimerMax)
            {
                spriteSheetAnimationData.ValueRW.FrameTimer -= spriteSheetAnimationData.ValueRO.FrameTimerMax;
                spriteSheetAnimationData.ValueRW.CurrentFrame =
                    (spriteSheetAnimationData.ValueRO.CurrentFrame + 1) % spriteSheetAnimationData.ValueRO.FrameCount;

                var uvScaleX = spriteSheetAnimationData.ValueRO.Uv.x;
                var uvOffsetX = uvScaleX * spriteSheetAnimationData.ValueRO.CurrentFrame;
                var uvScaleY = spriteSheetAnimationData.ValueRO.Uv.y;
                var uvOffsetY = spriteSheetAnimationData.ValueRO.Uv.w;
                var uv = new Vector4(uvScaleX, uvScaleY, uvOffsetX, uvOffsetY);

                spriteSheetAnimationData.ValueRW.Uv = uv;
            }

            var position = localToWorld.ValueRO.Position + spriteTransform.ValueRO.Position;
            var rotation = spriteTransform.ValueRO.Rotation;
            spriteSheetAnimationData.ValueRW.Matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
        }
    }
}
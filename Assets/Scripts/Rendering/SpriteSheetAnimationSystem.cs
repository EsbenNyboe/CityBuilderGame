using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(PathFollowSystem))]
public partial class SpriteSheetAnimationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (spriteSheetAnimationData, localToWorld, entity) in SystemAPI.Query<RefRW<SpriteSheetAnimation>, RefRW<LocalToWorld>>()
                     .WithEntityAccess())
        {
            spriteSheetAnimationData.ValueRW.FrameTimer += SystemAPI.Time.DeltaTime;
            while (spriteSheetAnimationData.ValueRO.FrameTimer > spriteSheetAnimationData.ValueRO.FrameTimerMax)
            {
                spriteSheetAnimationData.ValueRW.FrameTimer -= spriteSheetAnimationData.ValueRO.FrameTimerMax;
                spriteSheetAnimationData.ValueRW.CurrentFrame =
                    (spriteSheetAnimationData.ValueRO.CurrentFrame + 1) % spriteSheetAnimationData.ValueRO.FrameCount;

                var uvScaleX = 1f / spriteSheetAnimationData.ValueRO.FrameCount;
                var uvOffsetX = uvScaleX * spriteSheetAnimationData.ValueRO.CurrentFrame;
                var uvScaleY = spriteSheetAnimationData.ValueRO.Uv.y;
                var uvOffsetY = spriteSheetAnimationData.ValueRO.Uv.w;
                var uv = new Vector4(uvScaleX, uvScaleY, uvOffsetX, uvOffsetY);

                spriteSheetAnimationData.ValueRW.Uv = uv;
            }

            var position = localToWorld.ValueRO.Position;
            // sort sprites, by putting lower sprites in front of higher ones:
            // positon.z = positon.y * 0.01f;
            spriteSheetAnimationData.ValueRW.Matrix = Matrix4x4.TRS(position, localToWorld.ValueRO.Rotation, Vector3.one);
        }
    }
}
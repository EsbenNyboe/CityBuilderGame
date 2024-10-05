using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(SpriteSheetAnimationSystem))]
public partial class SpriteSheetRendererSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (spriteSheetAnimationData, localTransform) in SystemAPI.Query<RefRO<SpriteSheetAnimationData>, RefRO<LocalTransform>>())
        {
            var uvScaleX = 1f / spriteSheetAnimationData.ValueRO.FrameCount;
            var uvOffsetX = uvScaleX * spriteSheetAnimationData.ValueRO.CurrentFrame;
            var uvScaleY = 1f;
            var uvOffsetY = 0f;
            var uv = new Vector4(uvScaleX, uvScaleY, uvOffsetX, uvOffsetY);

            var materialPropertyBlock = new MaterialPropertyBlock();

            materialPropertyBlock.SetVector("_MainTex_ST", uv);
            Graphics.DrawMesh(SpriteSheetRendererManager.Instance.TestMesh, localTransform.ValueRO.Position, quaternion.identity,
                SpriteSheetRendererManager.Instance.TestMaterial, 0, Camera.main, 0, materialPropertyBlock);
        }
    }
}
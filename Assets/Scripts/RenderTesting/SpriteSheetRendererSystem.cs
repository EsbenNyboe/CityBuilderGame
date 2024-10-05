using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(SpriteSheetAnimationSystem))]
public partial class SpriteSheetRendererSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (spriteSheetAnimationData, localTransform) in SystemAPI.Query<RefRO<SpriteSheetAnimationData>, RefRO<LocalTransform>>())
        {
            var materialPropertyBlock = new MaterialPropertyBlock();
            materialPropertyBlock.SetVector("_MainTex_ST", spriteSheetAnimationData.ValueRO.Uv);
            Graphics.DrawMesh(SpriteSheetRendererManager.Instance.TestMesh, spriteSheetAnimationData.ValueRO.Matrix,
                SpriteSheetRendererManager.Instance.TestMaterial, 0, Camera.main, 0, materialPropertyBlock);
        }
    }
}
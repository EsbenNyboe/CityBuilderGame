using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateAfter(typeof(SpriteSheetAnimationSystem))]
public partial class SpriteSheetRendererSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var materialPropertyBlock = new MaterialPropertyBlock();
        var mesh = SpriteSheetRendererManager.Instance.TestMesh;
        var material = SpriteSheetRendererManager.Instance.TestMaterial;
        var camera = Camera.main;

        var entityQuery = GetEntityQuery(typeof(SpriteSheetAnimationData));

        var animationDataArray = entityQuery.ToComponentDataArray<SpriteSheetAnimationData>(Allocator.TempJob);

        if (animationDataArray.Length >= 1)
        {
            var matrixList = new List<Matrix4x4>();
            var uvList = new List<Vector4>();
            for (var i = 0; i < animationDataArray.Length; i++)
            {
                matrixList.Add(animationDataArray[i].Matrix);
                uvList.Add(animationDataArray[i].Uv);
            }

            materialPropertyBlock.SetVectorArray("_MainTex_UV", uvList.ToArray());
            Graphics.DrawMeshInstanced(mesh, 0, material, matrixList.ToArray(), matrixList.Count, materialPropertyBlock);
        }

        animationDataArray.Dispose();
    }
}
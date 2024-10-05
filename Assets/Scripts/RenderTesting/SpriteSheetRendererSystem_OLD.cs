using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(SpriteSheetAnimationSystem))]
[DisableAutoCreation]
public partial class SpriteSheetRendererSystem_OLD : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<SpriteSheetAnimationData>();
    }

    protected override void OnUpdate()
    {
        var materialPropertyBlock = new MaterialPropertyBlock();
        var mesh = SpriteSheetRendererManager.Instance.TestMesh;
        var material = SpriteSheetRendererManager.Instance.TestMaterial;

        var entityQuery = GetEntityQuery(typeof(SpriteSheetAnimationData), typeof(LocalTransform));

        var animationDataArray = entityQuery.ToComponentDataArray<SpriteSheetAnimationData>(Allocator.TempJob);
        var localTransformArray = entityQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

        for (var i = 0; i < localTransformArray.Length; i++)
        {
            for (var j = i + 1; j < localTransformArray.Length; j++)
            {
                if (localTransformArray[i].Position.y < localTransformArray[j].Position.y)
                {
                    // Swap
                    var localTransform = localTransformArray[i];
                    localTransformArray[i] = localTransformArray[j];
                    localTransformArray[j] = localTransform;

                    var spriteSheetAnimationData = animationDataArray[i];
                    animationDataArray[i] = animationDataArray[j];
                    animationDataArray[j] = spriteSheetAnimationData;
                }
            }
        }

        var matrixList = new List<Matrix4x4>();
        var uvList = new List<Vector4>();
        for (var i = 0; i < animationDataArray.Length; i++)
        {
            matrixList.Add(animationDataArray[i].Matrix);
            uvList.Add(animationDataArray[i].Uv);
        }

        materialPropertyBlock.SetVectorArray("_MainTex_UV", uvList.ToArray());
        Graphics.DrawMeshInstanced(mesh, 0, material, matrixList.ToArray(), matrixList.Count, materialPropertyBlock);

        animationDataArray.Dispose();
        localTransformArray.Dispose();
    }
}
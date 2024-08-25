using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public partial class AnimationTestSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var localTransform in SystemAPI.Query<RefRO<LocalTransform>>())
        {
            Graphics.DrawMesh(AnimationTestHandler.GetInstance().Mesh, localTransform.ValueRO.Position, Quaternion.identity, AnimationTestHandler.GetInstance().Material, 0);
        }
    }
}
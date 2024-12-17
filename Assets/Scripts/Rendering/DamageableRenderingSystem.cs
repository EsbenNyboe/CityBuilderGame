using GridEntityNS;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public partial class DamageableRenderingSystem : SystemBase
    {
        private static readonly Vector4[] UVInstancedArray = new Vector4[SliceCount];
        private static readonly Matrix4x4[] MatrixInstancedArray = new Matrix4x4[SliceCount];
        private static readonly int MainTexUV = Shader.PropertyToID("_MainTex_UV");
        private EntityQuery _damageableQuery;
        private static int SliceCount => 1023;

        protected override void OnCreate()
        {
            _damageableQuery = GetEntityQuery(typeof(Damageable), typeof(LocalTransform));
        }

        protected override void OnUpdate()
        {
            var damageableCount = _damageableQuery.CalculateEntityCount();
            var spriteUvArray = new NativeArray<Vector4>(damageableCount, Allocator.TempJob);
            var spriteMatrixArray = new NativeArray<Matrix4x4>(damageableCount, Allocator.TempJob);

            var damageableTransforms = _damageableQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var damageables = _damageableQuery.ToComponentDataArray<Damageable>(Allocator.TempJob);
            var createSpriteArraysJob = new CreateSpriteArraysJob
            {
                Damageables = damageables,
                DamageableTransforms = damageableTransforms,
                SpriteUvArray = spriteUvArray,
                SpriteMatrixArray = spriteMatrixArray
            };

            var mesh = DamageableRenderingConfig.Instance.Mesh;
            var material = DamageableRenderingConfig.Instance.Material;
            DrawMesh(mesh, material, spriteUvArray, spriteMatrixArray);

            damageableTransforms.Dispose();
            damageables.Dispose();
            spriteUvArray.Dispose();
            spriteMatrixArray.Dispose();
        }

        private static void DrawMesh(Mesh mesh, Material material, NativeArray<Vector4> uvArray,
            NativeArray<Matrix4x4> matrixArray)
        {
            var materialPropertyBlock = new MaterialPropertyBlock();

            for (var i = 0; i < matrixArray.Length; i += SliceCount)
            {
                var sliceSize = math.min(matrixArray.Length - i, SliceCount);
                NativeArray<Matrix4x4>.Copy(matrixArray, i, MatrixInstancedArray, 0, sliceSize);
                NativeArray<Vector4>.Copy(uvArray, i, UVInstancedArray, 0, sliceSize);

                materialPropertyBlock.SetVectorArray(MainTexUV, UVInstancedArray);
                Graphics.DrawMeshInstanced(mesh, 0, material, MatrixInstancedArray, sliceSize, materialPropertyBlock);
            }
        }

        private struct CreateSpriteArraysJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Damageable> Damageables;
            [ReadOnly] public NativeArray<LocalTransform> DamageableTransforms;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<Vector4> SpriteUvArray;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<Matrix4x4> SpriteMatrixArray;

            public void Execute(int index)
            {
                // SpriteUvArray[index] = new Vector4()
            }
        }
    }
}
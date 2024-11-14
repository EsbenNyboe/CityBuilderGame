using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public partial class SpriteSheetRendererSystem : SystemBase
    {
        private static readonly Vector4[] UVInstancedArray = new Vector4[SliceCount];
        private static readonly Matrix4x4[] MatrixInstancedArray = new Matrix4x4[SliceCount];
        private static readonly int MainTexUV = Shader.PropertyToID("_MainTex_UV");
        private static int SliceCount => 1023;

        protected override void OnCreate()
        {
            RequireForUpdate<SpriteSheetSortingManager>();
        }

        protected override void OnUpdate()
        {
            World.Unmanaged.GetExistingSystemState<SpriteSheetSortingSystem>().CompleteDependency();

            var spriteSheetSortingManager = SystemAPI.GetSingleton<SpriteSheetSortingManager>();
            var unitMesh = SpriteSheetRendererManager.Instance.UnitMesh;
            var unitMaterial = SpriteSheetRendererManager.Instance.UnitMaterial;

            // Setup uv's to select sprite-frame, then draw mesh for all instances
            DrawMesh(unitMesh, unitMaterial, spriteSheetSortingManager.SpriteUvArray,
                spriteSheetSortingManager.SpriteMatrixArray);
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
    }
}
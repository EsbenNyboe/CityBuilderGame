using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public partial class WorldSpriteSheetRendererSystem : SystemBase
    {
        private static readonly Vector4[] UVInstancedArray = new Vector4[SliceCount];
        private static readonly Matrix4x4[] MatrixInstancedArray = new Matrix4x4[SliceCount];
        private static readonly int MainTexUV = Shader.PropertyToID("_MainTex_UV");
        private static int SliceCount => 1023;

        protected override void OnCreate()
        {
            RequireForUpdate<WorldSpriteSheetSortingManager>();
        }

        protected override void OnUpdate()
        {
            World.Unmanaged.GetExistingSystemState<WorldSpriteSheetSortingManagerSystem>().CompleteDependency();

            var spriteSheetSortingManager = SystemAPI.GetSingleton<WorldSpriteSheetSortingManager>();
            var unitMesh = WorldSpriteSheetConfig.Instance.UnitMesh;
            var unitMaterial = WorldSpriteSheetConfig.Instance.UnitMaterial;

            // Setup uv's to select sprite-frame, then draw mesh for all instances
            DrawSlicedMesh(unitMesh, unitMaterial, spriteSheetSortingManager.SpriteUvArray,
                spriteSheetSortingManager.SpriteMatrixArray);
        }

        private static void DrawSlicedMesh(Mesh mesh, Material material, NativeArray<Vector4> uvArray,
            NativeArray<Matrix4x4> matrixArray)
        {
            var materialPropertyBlock = new MaterialPropertyBlock();

            for (var i = 0; i < matrixArray.Length; i += SliceCount)
            {
                var sliceSize = math.min(matrixArray.Length - i, SliceCount);
                NativeArray<Matrix4x4>.Copy(matrixArray, i, MatrixInstancedArray, 0, sliceSize);
                NativeArray<Vector4>.Copy(uvArray, i, UVInstancedArray, 0, sliceSize);

                DrawMesh(materialPropertyBlock, mesh, material, UVInstancedArray, MatrixInstancedArray);
            }
        }

        public static void DrawMesh(MaterialPropertyBlock materialPropertyBlock, Mesh mesh, Material material, Vector4[] uvArray,
            Matrix4x4[] matrix4X4Array)
        {
            materialPropertyBlock.SetVectorArray(MainTexUV, uvArray);
            Graphics.DrawMeshInstanced(mesh, 0, material, matrix4X4Array, uvArray.Length, materialPropertyBlock);
        }
    }
}
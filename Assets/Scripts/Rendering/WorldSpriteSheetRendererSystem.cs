using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public partial class WorldSpriteSheetRendererSystem : SystemBase
    {
        private static Vector4[] _uvInstancedArray;
        private static Matrix4x4[] _matrixInstancedArray;
        private static readonly int MainTexUV = Shader.PropertyToID("_MainTex_UV");
        private static int SliceCount => 1023;

        protected override void OnCreate()
        {
            RequireForUpdate<WorldSpriteSheetSortingManager>();
            _uvInstancedArray = new Vector4[SliceCount];
            _matrixInstancedArray = new Matrix4x4[SliceCount];
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
            for (var i = 0; i < matrixArray.Length; i += SliceCount)
            {
                var materialPropertyBlock = new MaterialPropertyBlock();

                var sliceSize = math.min(matrixArray.Length - i, SliceCount);

                NativeArray<Matrix4x4>.Copy(matrixArray, i, _matrixInstancedArray, 0, sliceSize);
                NativeArray<Vector4>.Copy(uvArray, i, _uvInstancedArray, 0, sliceSize);

                DrawMesh(materialPropertyBlock, mesh, material, _uvInstancedArray, _matrixInstancedArray, sliceSize);
            }
        }

        public static void DrawMesh(MaterialPropertyBlock materialPropertyBlock, Mesh mesh, Material material, Vector4[] uvArray,
            Matrix4x4[] matrix4X4Array, int count)
        {
            materialPropertyBlock.SetVectorArray(MainTexUV, uvArray);
            Graphics.DrawMeshInstanced(mesh, 0, material, matrix4X4Array, count, materialPropertyBlock);
        }
    }
}
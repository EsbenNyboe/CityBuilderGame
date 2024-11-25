using Unity.Entities;
using UnityEngine;

namespace Rendering
{
    public partial class WorldSpriteSheetRendererSystem : SystemBase
    {
        protected override void OnUpdate()
        {
        }

        public static void DrawMesh(Mesh mesh, Material material, Vector4[] uvArray,
            Matrix4x4[] matrix4X4Array)
        {
            var mainTexUV = Shader.PropertyToID("_MainTex_UV");
            var materialPropertyBlock = new MaterialPropertyBlock();

            materialPropertyBlock.SetVectorArray(mainTexUV, uvArray);
            Graphics.DrawMeshInstanced(mesh, 0, material, matrix4X4Array, uvArray.Length, materialPropertyBlock);
        }
    }
}
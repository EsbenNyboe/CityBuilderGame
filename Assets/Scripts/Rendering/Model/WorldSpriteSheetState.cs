using Unity.Entities;
using UnityEngine;

namespace Rendering
{
    public struct WorldSpriteSheetState : IComponentData
    {
        public Vector4 Uv;
        public Matrix4x4 Matrix;
    }
}
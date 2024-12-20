using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Rendering
{
    public struct WorldSpriteSheetSortingManager : ICleanupComponentData
    {
        public NativeArray<Matrix4x4> SpriteMatrixArray;
        public NativeArray<Vector4> SpriteUvArray;
    }
}
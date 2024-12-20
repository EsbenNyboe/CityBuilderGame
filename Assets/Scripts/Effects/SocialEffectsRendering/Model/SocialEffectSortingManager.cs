using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Effects.SocialEffectsRendering
{
    public struct SocialEffectSortingManager : IComponentData
    {
        public NativeQueue<SocialEffectData> SocialEffectQueue;
        public NativeArray<Matrix4x4> SpriteMatrixArray;
        public NativeArray<Vector4> SpriteUvArray;
        public float Scale;
        public float Offset;
        public float Lifetime;
        public float MoveSpeed;
    }

    public struct SocialEffectData
    {
        public Entity Entity;
        public float TimeCreated;
        public SocialEffectType Type;
    }
}
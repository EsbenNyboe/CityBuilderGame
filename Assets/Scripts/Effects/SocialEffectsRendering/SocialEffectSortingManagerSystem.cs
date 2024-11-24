using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Effects.SocialEffectsRendering
{
    public struct SocialEffectSortingManager : IComponentData
    {
        public NativeQueue<SocialEffectData> SocialEffectQueue;
        public NativeArray<Matrix4x4> SpriteMatrixArray;
        public NativeArray<Vector4> SpriteUvArray;
    }

    public struct SocialEffectData
    {
        public Entity Entity;
        public float TimeCreated;
        public SocialEffectType Type;
    }

    public partial struct SocialEffectSortingManagerSystem : ISystem
    {
        private const float Lifetime = 1f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SocialEffectSortingManager>();
            state.EntityManager.CreateSingleton<SocialEffectSortingManager>();
        }

        public void OnDestroy(ref SystemState state)
        {
            var socialEffectSortingManager = SystemAPI.GetSingleton<SocialEffectSortingManager>();
            socialEffectSortingManager.SocialEffectQueue.Dispose();
            socialEffectSortingManager.SpriteMatrixArray.Dispose();
            socialEffectSortingManager.SpriteUvArray.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var socialEffectSortingManager = SystemAPI.GetSingleton<SocialEffectSortingManager>();
            if (!socialEffectSortingManager.SocialEffectQueue.IsCreated)
            {
                socialEffectSortingManager.SocialEffectQueue = new NativeQueue<SocialEffectData>(Allocator.Persistent);
            }

            var killThreshold = (float)SystemAPI.Time.ElapsedTime - Lifetime;
            var queueIsDirty = true;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            while (queueIsDirty && socialEffectSortingManager.SocialEffectQueue.Count > 0)
            {
                if (socialEffectSortingManager.SocialEffectQueue.Peek().TimeCreated > killThreshold)
                {
                    queueIsDirty = false;
                }
                else
                {
                    var socialEffectData = socialEffectSortingManager.SocialEffectQueue.Dequeue();
                    ecb.DestroyEntity(socialEffectData.Entity);
                }
            }

            PrepareRenderingData(ref state, ref socialEffectSortingManager);
            SystemAPI.SetSingleton(socialEffectSortingManager);
        }

        private void PrepareRenderingData(ref SystemState state,
            ref SocialEffectSortingManager socialEffectSortingManager)
        {
            if (socialEffectSortingManager.SpriteMatrixArray.IsCreated)
            {
                socialEffectSortingManager.SpriteMatrixArray.Dispose();
            }

            if (socialEffectSortingManager.SpriteUvArray.IsCreated)
            {
                socialEffectSortingManager.SpriteUvArray.Dispose();
            }

            var length = socialEffectSortingManager.SocialEffectQueue.Count;

            socialEffectSortingManager.SpriteMatrixArray = new NativeArray<Matrix4x4>(length, Allocator.Persistent);
            socialEffectSortingManager.SpriteUvArray = new NativeArray<Vector4>(length, Allocator.Persistent);

            var lookup = SystemAPI.GetComponentLookup<SocialEffect>();

            for (var i = 0; i < length; i++)
            {
                var socialEffectData = socialEffectSortingManager.SocialEffectQueue.Dequeue();
                socialEffectSortingManager.SpriteMatrixArray[i] =
                    Matrix4x4.TRS(lookup[socialEffectData.Entity].Position, quaternion.identity, Vector3.one);

                var spriteColumns = 1;
                var spriteRows = 2;
                var uvScaleX = 1f / spriteColumns;
                var uvScaleY = 1f / spriteRows;
                var spriteRow = socialEffectData.Type == SocialEffectType.Positive ? 0 : 1;
                socialEffectSortingManager.SpriteUvArray[i] = new Vector4(uvScaleX, uvScaleY, 0, uvScaleY * spriteRow);

                socialEffectSortingManager.SocialEffectQueue.Enqueue(socialEffectData);
            }
        }
    }
}
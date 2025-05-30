using CustomTimeCore;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Effects.SocialEffectsRendering
{
    public partial struct SocialEffectSortingManagerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
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

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            var socialEffectSortingManager = SystemAPI.GetSingleton<SocialEffectSortingManager>();
            if (!socialEffectSortingManager.SocialEffectQueue.IsCreated)
            {
                socialEffectSortingManager.SocialEffectQueue = new NativeQueue<SocialEffectData>(Allocator.Persistent);
            }

            var killThreshold = (float)SystemAPI.Time.ElapsedTime * timeScale - socialEffectSortingManager.Lifetime * timeScale;
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
                var position = lookup[socialEffectData.Entity].Position;
                position.y += socialEffectSortingManager.Offset;
                position.z -= 1;
                var scale = Vector3.one * socialEffectSortingManager.Scale;
                socialEffectSortingManager.SpriteMatrixArray[i] =
                    Matrix4x4.TRS(position, quaternion.identity, scale);

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
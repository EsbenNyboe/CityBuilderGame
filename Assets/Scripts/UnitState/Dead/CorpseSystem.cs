using GridEntityNS;
using Rendering;
using UnitBehaviours.UnitManagers;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace UnitState.Dead
{
    public partial struct CorpseSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<WorldSpriteSheetManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var worldSpriteSheetManager = SystemAPI.GetSingleton<WorldSpriteSheetManager>();
            if (!worldSpriteSheetManager.IsInitialized())
            {
                return;
            }

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var corpseFrames = worldSpriteSheetManager.GetAnimationLength(WorldSpriteSheetEntryType.BoarDead);

            foreach (var (corpse, localTransform, entity) in SystemAPI.Query<RefRO<Corpse>, RefRO<LocalTransform>>()
                         .WithEntityAccess()
                         .WithNone<WorldSpriteSheetState>())
            {
                ecb.AddComponent(entity, new WorldSpriteSheetState
                {
                    Uv = worldSpriteSheetManager.GetUv(WorldSpriteSheetEntryType.BoarDead,
                        GetCorpseFrame(corpseFrames, corpse.ValueRO.MeatCurrent, corpse.ValueRO.MeatMax)),
                    Matrix = Matrix4x4.TRS(localTransform.ValueRO.Position, localTransform.ValueRO.Rotation, Vector3.one)
                });
                ecb.AddComponent<GridEntity>(entity); // TODO: Do not use grid entity for this!
            }

            foreach (var (corpse, worldSpriteSheetState) in SystemAPI
                         .Query<RefRW<Corpse>, RefRW<WorldSpriteSheetState>>())
            {
                worldSpriteSheetState.ValueRW.Uv = worldSpriteSheetManager.GetUv(WorldSpriteSheetEntryType.BoarDead,
                    GetCorpseFrame(corpseFrames, corpse.ValueRO.MeatCurrent, corpse.ValueRO.MeatMax));
            }
        }

        private int GetCorpseFrame(int corpseFrames, int meatCurrent, int meatMax)
        {
            var meatPercentage = (float)meatCurrent / meatMax;
            var corpseFrameProgress = 1 - meatPercentage;
            var corpseFrame = Mathf.Min(corpseFrames - 1, Mathf.FloorToInt(corpseFrameProgress * corpseFrames));
            return corpseFrame;
        }
    }
}
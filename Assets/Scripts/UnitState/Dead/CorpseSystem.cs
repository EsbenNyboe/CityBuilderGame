using CustomTimeCore;
using GridEntityNS;
using Rendering;
using UnitBehaviours.UnitManagers;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitState.Dead
{
    public struct Corpse : IComponentData
    {
        public float TimeOfDeath;
    }

    public partial struct CorpseSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<WorldSpriteSheetManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            var worldSpriteSheetManager = SystemAPI.GetSingleton<WorldSpriteSheetManager>();
            if (!worldSpriteSheetManager.IsInitialized())
            {
                return;
            }

            var decompositionDuration = SystemAPI.GetSingleton<UnitBehaviourManager>().DecompositionDuration * timeScale;
            var timeOfDecomposition = (float)SystemAPI.Time.ElapsedTime * timeScale - decompositionDuration;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var deathFrames = worldSpriteSheetManager.GetAnimationLength(WorldSpriteSheetEntryType.BoarDead);

            foreach (var (corpse, localTransform, entity) in SystemAPI.Query<RefRO<Corpse>, RefRO<LocalTransform>>()
                         .WithEntityAccess()
                         .WithNone<WorldSpriteSheetState>())
            {
                ecb.AddComponent(entity, new WorldSpriteSheetState
                {
                    Uv = worldSpriteSheetManager.GetUv(WorldSpriteSheetEntryType.BoarDead,
                        GetDeathFrame(deathFrames, corpse.ValueRO.TimeOfDeath, timeOfDecomposition, decompositionDuration)),
                    Matrix = Matrix4x4.TRS(localTransform.ValueRO.Position, localTransform.ValueRO.Rotation, Vector3.one)
                });
                ecb.AddComponent<GridEntity>(entity);
            }

            foreach (var (corpse, worldSpriteSheetState, entity) in SystemAPI
                         .Query<RefRO<Corpse>, RefRW<WorldSpriteSheetState>>().WithEntityAccess())
            {
                worldSpriteSheetState.ValueRW.Uv = worldSpriteSheetManager.GetUv(WorldSpriteSheetEntryType.BoarDead,
                    GetDeathFrame(deathFrames, corpse.ValueRO.TimeOfDeath, timeOfDecomposition, decompositionDuration));

                if (corpse.ValueRO.TimeOfDeath < timeOfDecomposition)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }

        private int GetDeathFrame(int deathFrames, float timeOfDeath, float timeOfDecomposition, float decompositionDuration)
        {
            var timeLeft = math.max(0, timeOfDeath - timeOfDecomposition);
            var timeLeftNormalized = timeLeft / decompositionDuration;
            timeLeftNormalized *= deathFrames;
            return math.max(0, deathFrames - 1 - Mathf.FloorToInt(timeLeftNormalized));
        }
    }
}
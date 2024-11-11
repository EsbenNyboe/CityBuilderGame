using Rendering;
using UnitAgency;
using UnitState;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace UnitBehaviours.Animosity
{
    public struct IsMurdering : IComponentData
    {
        public float TimeSinceStartedMurdering;
        public Entity Victim;
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    [BurstCompile]
    public partial struct IsMurderingSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;
        private const float MurderDuration = 0.1f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var ecb = GetEntityCommandBuffer(ref state);

            foreach (var (isMurdering, socialRelationships, entity) in SystemAPI
                         .Query<RefRW<IsMurdering>, RefRW<SocialRelationships>>().WithEntityAccess())
            {
                isMurdering.ValueRW.TimeSinceStartedMurdering += Time.deltaTime;
                if (isMurdering.ValueRO.TimeSinceStartedMurdering >= MurderDuration)
                {
                    // Murdering time!
                    var victimEntity = isMurdering.ValueRO.Victim;
                    if (victimEntity == Entity.Null || !SystemAPI.Exists(victimEntity))
                    {
                        // Oh, someone else murdered my victim... That makes me happy!
                        socialRelationships.ValueRW.HasAnimosity = false;
                        ecb.RemoveComponent<IsMurdering>(entity);
                        ecb.AddComponent<IsDeciding>(entity);
                        continue;
                    }

                    if (!SystemAPI.HasComponent<LocalTransform>(victimEntity))
                    {
                        // Ehhh... where did he go?
                        DebugHelper.LogError("Ehhh... where did he go?");
                        socialRelationships.ValueRW.HasAnimosity = false;
                        ecb.RemoveComponent<IsMurdering>(entity);
                        ecb.AddComponent<IsDeciding>(entity);
                        continue;
                    }

                    // There's no escape! I will murder now!
                    var victimLocalTransform = SystemAPI.GetComponent<LocalTransform>(victimEntity);
                    var victimPosition = victimLocalTransform.Position;
                    gridManager.DestroyUnit(ecb, victimEntity, victimPosition);
                    ecb.AddComponent(ecb.CreateEntity(), new DeathAnimationEvent
                    {
                        Position = victimPosition
                    });

                    // Ahh, good to get that out of my system. Now I can be civilized again.
                    socialRelationships.ValueRW.HasAnimosity = false;
                    ecb.RemoveComponent<IsMurdering>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }

            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
        }

        [BurstCompile]
        private EntityCommandBuffer GetEntityCommandBuffer(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            return ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        }
    }
}
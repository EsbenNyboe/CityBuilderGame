using UnitAgency;
using UnitState;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.Animosity
{
    public struct IsSeekingVictim : IComponentData
    {
        public bool HasStartedSeeking;
        public Entity Victim;
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsSeekingVictimSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            using var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (localTransform, pathFollow, socialRelationships, isSeekingVictim, entity) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<SocialRelationships>,
                             RefRW<IsSeekingVictim>>().WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    var victim = isSeekingVictim.ValueRO.Victim;
                    if (victim == entity)
                    {
                        DebugHelper.LogError("LOL: I'm trying to kill myself?");
                    }

                    if (victim == Entity.Null)
                    {
                        continue;
                    }

                    if (!SystemAPI.Exists(victim))
                    {
                        // Oh, someone else killed my victim! That makes me happy!
                        ecb.RemoveComponent<IsSeekingVictim>(entity);
                        ecb.AddComponent<IsDeciding>(entity);
                        continue;
                    }

                    if (!SystemAPI.HasComponent<LocalTransform>(victim))
                    {
                        if (SystemAPI.HasComponent<SocialRelationships>(victim))
                        {
                            DebugHelper.LogError("WTF: not cleaned up");
                        }
                        else
                        {
                            DebugHelper.LogError("WTF is this?");
                        }
                    }

                    var position = localTransform.ValueRO.Position;
                    var victimPosition = SystemAPI.GetComponent<LocalTransform>(victim).Position;
                    if (math.distance(position, victimPosition) < 1f)
                    {
                        // I'm close enough to kill my victim!!
                        ecb.RemoveComponent<IsSeekingVictim>(entity);
                        ecb.AddComponent<IsDeciding>(entity);
                    }

                    continue;
                }

                if (isSeekingVictim.ValueRO.HasStartedSeeking)
                {
                    // I arrived at my destination, but... I lost track of my victim...
                    ecb.RemoveComponent<IsSeekingVictim>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                // I must seek my victim! Who do I hate the most?
                var worstFondness = 0f;
                var mostHatedUnit = Entity.Null;
                foreach (var socialRelationship in socialRelationships.ValueRO.Relationships)
                {
                    if (socialRelationship.Value < worstFondness)
                    {
                        worstFondness = socialRelationship.Value;
                        mostHatedUnit = socialRelationship.Key;
                    }
                }

                if (worstFondness >= 0)
                {
                    // I don't hate anyone, actually... What was I thinking?
                    socialRelationships.ValueRW.HasAnimosity = false;
                    ecb.RemoveComponent<IsSeekingVictim>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
                else
                {
                    // I know who I want to kill!! Let's go kill him!!
                    isSeekingVictim.ValueRW.HasStartedSeeking = true;
                    isSeekingVictim.ValueRW.Victim = mostHatedUnit;
                    var cell = GridHelpers.GetXY(localTransform.ValueRO.Position);
                    var victimCell = GridHelpers.GetXY(SystemAPI.GetComponent<LocalTransform>(mostHatedUnit).Position);
                    int2 targetCell = -1;

                    if (gridManager.IsWalkable(victimCell))
                    {
                        // Muahaha, I know just how to get to him! 
                        PathHelpers.TrySetPath(ecb, entity, cell, victimCell);
                    }
                    else if (gridManager.TryGetNearbyEmptyCellSemiRandom(victimCell, out var nearbyCell))
                    {
                        // I can't get to him right now... I'll try and go as close as I can!
                        PathHelpers.TrySetPath(ecb, entity, cell, nearbyCell);
                    }
                    else
                    {
                        // There's no way I can get to him! I give up... I mean..
                        // Sometimes you just gotta let go, and take a deep breath... Life goes on..
                        socialRelationships.ValueRW.HasAnimosity = false;
                        ecb.RemoveComponent<IsSeekingVictim>(entity);
                        ecb.AddComponent<IsDeciding>(entity);
                    }
                }
            }

            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
            ecb.Playback(state.EntityManager);
        }
    }
}
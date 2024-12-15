using Rendering;
using SystemGroups;
using UnitBehaviours;
using UnitBehaviours.Idle;
using UnitBehaviours.Pathing;
using UnitBehaviours.Targeting;
using UnitSpawn;
using UnitState.Mood;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitAgency
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup), OrderLast = true)]
    public partial struct BoarDecisionMakingSystem : ISystem
    {
        private ComponentLookup<RandomContainer> _randomContainerLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<QuadrantDataManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _randomContainerLookup = SystemAPI.GetComponentLookup<RandomContainer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _randomContainerLookup.Update(ref state);
            var quadrantDataManager = SystemAPI.GetSingleton<QuadrantDataManager>();
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var unitBehaviourManager = SystemAPI.GetSingleton<UnitBehaviourManager>();
            var quadrantsToSearch = GridHelpers.CalculatePositionListLength(unitBehaviourManager.BoarQuadrantRange);

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, _, pathFollow, localTransform, moodInitiative, entity) in SystemAPI
                         .Query<RefRO<Boar>, RefRO<IsDeciding>, RefRO<PathFollow>, RefRO<LocalTransform>, RefRW<MoodInitiative>>()
                         .WithEntityAccess().WithNone<Pathfinding>().WithNone<AttackAnimation>())
            {
                ecb.RemoveComponent<IsDeciding>(entity);

                var position = localTransform.ValueRO.Position;

                if (!pathFollow.ValueRO.IsMoving() &&
                    QuadrantSystem.TryFindClosestEntity(quadrantDataManager.VillagerQuadrantMap, gridManager, quadrantsToSearch, position, entity,
                        out var closestTargetEntity, out var closestTargetDistance))
                {
                    if (closestTargetDistance <= IsAttemptingMurderSystem.AttackRange)
                    {
                        ecb.AddComponent(entity, new IsMurdering
                        {
                            Target = closestTargetEntity
                        });
                        ecb.AddComponent(entity, new AttackAnimation(new int2(-1), -1));
                    }
                    else
                    {
                        if (!moodInitiative.ValueRO.HasInitiative())
                        {
                            ecb.AddComponent<IsIdle>(entity);
                        }
                        else
                        {
                            var randomDelay = _randomContainerLookup[entity].Random.NextFloat(0, 1);
                            ecb.SetComponent(entity, new ActionGate
                            {
                                MinTimeOfAction = (float)SystemAPI.Time.ElapsedTime + randomDelay
                            });
                            moodInitiative.ValueRW.UseInitiative();
                            ecb.AddComponent<BoarIsCharging>(entity);
                        }
                    }
                }
                else
                {
                    ecb.AddComponent<IsIdle>(entity);
                }
            }
        }
    }
}
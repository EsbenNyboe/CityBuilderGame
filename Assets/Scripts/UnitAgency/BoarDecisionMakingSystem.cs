using UnitBehaviours;
using UnitBehaviours.Pathing;
using UnitBehaviours.Targeting;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitAgency
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup), OrderLast = true)]
    public partial struct BoarDecisionMakingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<QuadrantDataManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var quadrantDataManager = SystemAPI.GetSingleton<QuadrantDataManager>();
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, _, pathFollow, localTransform, entity) in SystemAPI
                         .Query<RefRO<Boar>, RefRO<IsDeciding>, RefRO<PathFollow>, RefRO<LocalTransform>>()
                         .WithEntityAccess().WithNone<Pathfinding>().WithNone<AttackAnimation>())
            {
                ecb.RemoveComponent<IsDeciding>(entity);

                var position = localTransform.ValueRO.Position;
                var cell = GridHelpers.GetXY(position);

                if (!pathFollow.ValueRO.IsMoving() &&
                    QuadrantSystem.TryFindClosestEntity(quadrantDataManager.QuadrantMultiHashMap,
                        QuadrantSystem.GetPositionHashMapKey(position),
                        gridManager.GetSection(cell), position, entity, out var closestTargetEntity, out var closestTargetDistance))
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
                        ecb.AddComponent(entity, new IsAttemptingMurder());
                        ecb.SetComponent(entity, new TargetFollow
                        {
                            Target = closestTargetEntity,
                            CurrentDistanceToTarget = closestTargetDistance,
                            DesiredRange = IsAttemptingMurderSystem.AttackRange
                        });
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
using UnitState;
using Unity.Entities;

namespace UnitBehaviours.Pathing
{
    public struct IsAttemptingMurder : IComponentData
    {
    }

    public partial struct IsAttemptingMurderSystem : ISystem
    {
        private const float AttackRange = 0.5f;

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (targetFollow, _) in SystemAPI.Query<RefRO<TargetFollow>, RefRO<IsAttemptingMurder>>())
            {
                if (targetFollow.ValueRO.Target != Entity.Null &&
                    targetFollow.ValueRO.CurrentDistanceToTarget < AttackRange)
                {
                    SystemAPI.SetComponentEnabled<IsAlive>(targetFollow.ValueRO.Target, false);
                }
            }
        }
    }
}
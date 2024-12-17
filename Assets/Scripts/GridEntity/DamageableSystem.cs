using Grid;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace GridEntityNS
{
    public struct Damageable : IComponentData
    {
        public float HealthNormalized;
    }

    public partial struct DamageableSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
        }

        public void OnUpdate(ref SystemState state)
        {
            new SetDamageableStateJob
            {
                GridManager = SystemAPI.GetSingleton<GridManager>()
            }.ScheduleParallel(state.Dependency).Complete();
        }

        [BurstCompile]
        private partial struct SetDamageableStateJob : IJobEntity
        {
            [ReadOnly] public GridManager GridManager;

            public void Execute(in GridEntity _, in LocalTransform localTransform, ref Damageable damageable)
            {
                var gridIndex = GridManager.GetIndex(localTransform.Position);
                var health = GridManager.GetHealthNormalized(gridIndex);
                damageable.HealthNormalized = health;
            }
        }
    }
}
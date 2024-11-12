using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace UnitState
{
    public partial class IsAliveSystem : SystemBase
    {
        private EntityQuery _deadUnits;

        protected override void OnCreate()
        {
            RequireForUpdate<SocialRelationships>();
            RequireForUpdate<IsAlive>();

            _deadUnits = new EntityQueryBuilder(Allocator.Temp).WithDisabled<IsAlive>().WithAll<Child>().Build(this);
        }

        protected override void OnUpdate()
        {
            using var deadUnits = _deadUnits.ToEntityArray(Allocator.Temp);

            // Cleanup dead units
            foreach (var socialRelationships in
                     SystemAPI.Query<RefRW<SocialRelationships>>().WithDisabled<IsAlive>())
            {
                socialRelationships.ValueRW.Relationships.Dispose();
            }

            // Cleanup alive units
            foreach (var socialRelationships in
                     SystemAPI.Query<RefRW<SocialRelationships>>().WithAll<IsAlive>())
            {
                foreach (var deadUnit in deadUnits)
                {
                    socialRelationships.ValueRW.Relationships.Remove(deadUnit);
                }
            }

            // Mark child-entities as dead
            using var deadEntitiesTotal = new NativeList<Entity>(deadUnits.Length * 2, Allocator.Temp);
            deadEntitiesTotal.AddRange(deadUnits);
            foreach (var child in SystemAPI.Query<DynamicBuffer<Child>>().WithDisabled<IsAlive>())
            {
                deadEntitiesTotal.Add(child[0].Value);
            }

            // Destroy dead units
            EntityManager.DestroyEntity(deadEntitiesTotal.AsArray());
        }
    }
}
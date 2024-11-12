using UnitBehaviours.Pathing;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace UnitState
{
    public partial class IsAliveSystem : SystemBase
    {
        private EntityQuery _deadUnits;
        private SystemHandle _gridManagerSystemHandle;

        protected override void OnCreate()
        {
            RequireForUpdate<SocialRelationships>();
            RequireForUpdate<IsAlive>();

            _deadUnits = new EntityQueryBuilder(Allocator.Temp).WithDisabled<IsAlive>().WithAll<Child>().Build(this);
            _gridManagerSystemHandle = World.GetOrCreateSystem<GridManagerSystem>();
        }

        protected override void OnUpdate()
        {
            var gridManagerRW = EntityManager.GetComponentDataRW<GridManager>(_gridManagerSystemHandle);
            using var deadUnits = _deadUnits.ToEntityArray(Allocator.Temp);

            // Cleanup grid
            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithDisabled<IsAlive>()
                         .WithEntityAccess())
            {
                var position = localTransform.ValueRO.Position;
                gridManagerRW.ValueRW.OnUnitDestroyed(entity, position);
            }

            // Cleanup dead units relationships
            foreach (var socialRelationships in
                     SystemAPI.Query<RefRW<SocialRelationships>>().WithDisabled<IsAlive>())
            {
                socialRelationships.ValueRW.Relationships.Dispose();
            }

            // Cleanup alive units relationships
            foreach (var socialRelationships in
                     SystemAPI.Query<RefRW<SocialRelationships>>().WithAll<IsAlive>())
            {
                foreach (var deadUnit in deadUnits)
                {
                    socialRelationships.ValueRW.Relationships.Remove(deadUnit);
                }
            }

            // Cleanup alive units targets
            foreach (var targetFollow in SystemAPI.Query<RefRW<TargetFollow>>())
            {
                foreach (var deadUnit in deadUnits)
                {
                    if (targetFollow.ValueRO.Target == deadUnit)
                    {
                        targetFollow.ValueRW.Target = Entity.Null;
                    }
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
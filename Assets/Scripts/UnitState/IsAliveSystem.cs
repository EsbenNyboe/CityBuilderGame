using Rendering;
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
        private EntityQuery _deadZombies;

        protected override void OnCreate()
        {
            RequireForUpdate<IsAlive>();

            _deadZombies = new EntityQueryBuilder(Allocator.Temp).WithDisabled<IsAlive>().WithAll<Zombie>().Build(this);
            _deadUnits = new EntityQueryBuilder(Allocator.Temp).WithDisabled<IsAlive>().WithAll<Child>().Build(this);
            _gridManagerSystemHandle = World.GetOrCreateSystem<GridManagerSystem>();
        }

        protected override void OnUpdate()
        {
            var gridManagerRW = EntityManager.GetComponentDataRW<GridManager>(_gridManagerSystemHandle);
            using var deadUnits = _deadUnits.ToEntityArray(Allocator.Temp);
            using var ecb = new EntityCommandBuffer(WorldUpdateAllocator);

            // Play death effect
            foreach (var localTransform in SystemAPI.Query<RefRO<LocalTransform>>().WithDisabled<IsAlive>())
            {
                ecb.AddComponent(ecb.CreateEntity(),
                    new DeathAnimationEvent { Position = localTransform.ValueRO.Position });
            }

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
            using var deadLogs = new NativeList<Entity>(deadUnits.Length, Allocator.Temp);
            foreach (var child in SystemAPI.Query<DynamicBuffer<Child>>().WithDisabled<IsAlive>())
            {
                deadLogs.Add(child[0].Value);
            }

            // Destroy dead units
            ecb.Playback(EntityManager);
            EntityManager.DestroyEntity(deadUnits);
            EntityManager.DestroyEntity(deadLogs.AsArray());
            EntityManager.DestroyEntity(_deadZombies);
        }
    }
}
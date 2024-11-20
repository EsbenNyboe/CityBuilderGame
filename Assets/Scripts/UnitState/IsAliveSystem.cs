using Rendering;
using UnitBehaviours.Pathing;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace UnitState
{
    public struct IsAlive : IComponentData, IEnableableComponent
    {
    }

    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public partial struct IsAliveSystem : ISystem
    {
        private EntityQuery _deadUnits;
        private SystemHandle _gridManagerSystemHandle;
        private EntityQuery _deadZombies;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<IsAlive>();

            _deadZombies = new EntityQueryBuilder(Allocator.Temp)
                .WithDisabled<IsAlive>().WithAll<Zombie>().Build(ref state);
            _deadUnits = new EntityQueryBuilder(Allocator.Temp)
                .WithDisabled<IsAlive>().WithAll<Child>().Build(ref state);
            _gridManagerSystemHandle = state.World.GetOrCreateSystem<GridManagerSystem>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gridManagerRW = state.EntityManager.GetComponentDataRW<GridManager>(_gridManagerSystemHandle);
            using var deadUnits = _deadUnits.ToEntityArray(Allocator.Temp);
            using var invalidSocialEvents = new NativeList<Entity>(Allocator.Temp);
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Cleanup social events
            foreach (var (socialEvent, entity) in SystemAPI.Query<RefRO<SocialEvent>>().WithEntityAccess())
            {
                for (var i = 0; i < deadUnits.Length; i++)
                {
                    if (socialEvent.ValueRO.Perpetrator == deadUnits[i])
                    {
                        invalidSocialEvents.Add(entity);
                    }
                }
            }

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
            state.EntityManager.DestroyEntity(deadUnits);
            state.EntityManager.DestroyEntity(deadLogs.AsArray());
            state.EntityManager.DestroyEntity(_deadZombies);
            state.EntityManager.DestroyEntity(invalidSocialEvents.AsArray());
        }
    }
}
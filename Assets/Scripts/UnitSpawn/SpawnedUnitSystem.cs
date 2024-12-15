using Unity.Burst;
using Unity.Entities;

namespace UnitSpawn
{
    public struct SpawnedUnit : IComponentData
    {
    }

    [UpdateInGroup(typeof(LifetimeSystemGroup), OrderLast = true)]
    public partial struct SpawnedUnitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<SpawnedUnit>>().WithEntityAccess())
            {
                ecb.RemoveComponent<SpawnedUnit>(entity);
            }
        }
    }
}
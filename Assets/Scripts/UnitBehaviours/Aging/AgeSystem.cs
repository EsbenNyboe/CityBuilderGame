using CustomTimeCore;
using UnitBehaviours.UnitConfigurators;
using UnitBehaviours.UnitManagers;
using Unity.Entities;

namespace UnitBehaviours.Aging
{
    public partial struct AgeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<CustomTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var childHoodDuration = SystemAPI.GetSingleton<UnitBehaviourManager>().ChildHoodDuration;
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            foreach (var age in SystemAPI.Query<RefRW<Age>>())
            {
                age.ValueRW.AgeInSeconds += SystemAPI.Time.DeltaTime * timeScale;
            }

            foreach (var (age, entity) in SystemAPI.Query<RefRO<Age>>().WithAll<Baby>().WithEntityAccess())
            {
                if (age.ValueRO.AgeInSeconds > childHoodDuration)
                {
                    ecb.RemoveComponent<Baby>(entity);
                }
            }
        }
    }
}
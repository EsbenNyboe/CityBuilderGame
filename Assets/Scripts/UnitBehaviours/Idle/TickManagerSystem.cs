using SystemGroups;
using Unity.Burst;
using Unity.Entities;

namespace UnitBehaviours.Idle
{
    public struct TickManager : IComponentData
    {
        public float TimeSinceLastTick;
        public bool IsTicking;
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct TickManagerSystem : ISystem
    {
        private const float TimeBetweenTicks = 1f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<TickManager>(state.SystemHandle);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tickManager = SystemAPI.GetComponent<TickManager>(state.SystemHandle);
            tickManager.TimeSinceLastTick += SystemAPI.Time.DeltaTime;
            tickManager.IsTicking = false;
            if (tickManager.TimeSinceLastTick > TimeBetweenTicks)
            {
                tickManager.TimeSinceLastTick = 0;
                tickManager.IsTicking = true;
            }

            SystemAPI.SetComponent(state.SystemHandle, tickManager);
        }
    }
}
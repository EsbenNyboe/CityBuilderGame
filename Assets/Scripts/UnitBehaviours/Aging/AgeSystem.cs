using CustomTimeCore;
using Unity.Entities;

namespace UnitBehaviours.Aging
{
    public partial struct AgeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            foreach (var age in SystemAPI.Query<RefRW<Age>>())
            {
                age.ValueRW.AgeInSeconds += SystemAPI.Time.DeltaTime * timeScale;
            }
        }
    }
}
using Unity.Entities;

namespace CustomTime
{
    public partial struct CustomTimeManagerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTimeManager>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var customTimeManager = SystemAPI.GetSingleton<CustomTimeManager>();
            customTimeManager.TimeScale = CustomTimeUI.Instance.TimeScale;
            SystemAPI.SetSingleton(customTimeManager);
        }
    }
}
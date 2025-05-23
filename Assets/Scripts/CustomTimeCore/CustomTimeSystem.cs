using Unity.Entities;

namespace CustomTimeCore
{
    public partial struct CustomTimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var customTime = SystemAPI.GetSingleton<CustomTime>();
            customTime.TimeScale = CustomTimeUI.Instance.TimeScale;
            SystemAPI.SetSingleton(customTime);
        }
    }
}
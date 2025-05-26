using Unity.Entities;

namespace CustomTimeCore
{
    public partial class CustomTimeSystem : SystemBase
    {
        protected override void OnCreate()
        {
            EntityManager.CreateSingleton<CustomTime>();
        }

        protected override void OnUpdate()
        {
            var customTime = SystemAPI.GetSingleton<CustomTime>();
            customTime.TimeScale = CustomTimeUI.Instance.TimeScale;
            SystemAPI.SetSingleton(customTime);
        }
    }
}
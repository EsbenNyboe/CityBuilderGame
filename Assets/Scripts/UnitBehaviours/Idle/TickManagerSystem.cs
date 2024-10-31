using Unity.Entities;

public struct TickManager : IComponentData
{
    public float TimeSinceLastTick;
    public bool IsTicking;
}

public partial class TickManagerSystem : SystemBase
{
    private const float TimeBetweenTicks = 1f;

    protected override void OnCreate()
    {
        EntityManager.AddComponent<TickManager>(SystemHandle);
    }

    protected override void OnUpdate()
    {
        var tickManager = SystemAPI.GetComponent<TickManager>(SystemHandle);
        tickManager.TimeSinceLastTick += SystemAPI.Time.DeltaTime;
        tickManager.IsTicking = false;
        if (tickManager.TimeSinceLastTick > TimeBetweenTicks)
        {
            tickManager.TimeSinceLastTick = 0;
            tickManager.IsTicking = true;
        }

        SystemAPI.SetComponent(SystemHandle, tickManager);
    }
}
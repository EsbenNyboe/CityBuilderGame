using Unity.Entities;

public struct ChopAnimationManager : IComponentData
{
    public float ChopDuration;
    public float DamagePerChop;
    public float ChopAnimationSize;
    public float ChopAnimationIdleTime;
}

public partial class ChopAnimationManagerSystem : SystemBase
{
    protected override void OnCreate()
    {
        EntityManager.AddComponent<ChopAnimationManager>(SystemHandle);
    }

    protected override void OnUpdate()
    {
        SetChopAnimationValues();
    }

    private void SetChopAnimationValues()
    {
        SystemAPI.SetComponent(SystemHandle, new ChopAnimationManager
        {
            ChopDuration = ChopAnimationManagerConfig.ChopDuration(),
            DamagePerChop = ChopAnimationManagerConfig.DamagePerChop(),
            ChopAnimationSize = ChopAnimationManagerConfig.ChopAnimationSize(),
            ChopAnimationIdleTime = ChopAnimationManagerConfig.ChopAnimationPostIdleTimeNormalized()
        });
    }
}
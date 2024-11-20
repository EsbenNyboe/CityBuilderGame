using Unity.Entities;

public struct AttackAnimationManager : IComponentData
{
    public float AttackDuration;
    public float AttackAnimationSize;
    public float AttackAnimationIdleTime;
}

[UpdateInGroup(typeof(AnimationSystemGroup))]
public partial class AttackAnimationManagerSystem : SystemBase
{
    protected override void OnCreate()
    {
        EntityManager.CreateSingleton<AttackAnimationManager>();
    }

    protected override void OnUpdate()
    {
        SystemAPI.SetSingleton(new AttackAnimationManager
        {
            AttackDuration = AttackAnimationManagerConfig.ChopDuration(),
            AttackAnimationSize = AttackAnimationManagerConfig.ChopAnimationSize(),
            AttackAnimationIdleTime = AttackAnimationManagerConfig.ChopAnimationPostIdleTimeNormalized()
        });
    }
}
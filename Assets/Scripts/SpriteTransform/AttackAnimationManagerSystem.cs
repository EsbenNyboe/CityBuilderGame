using CustomTimeCore;
using SystemGroups;
using Unity.Entities;

namespace SpriteTransformNS
{
    [UpdateInGroup(typeof(AnimationSystemGroup))]
    public partial class AttackAnimationManagerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            EntityManager.CreateSingleton<AttackAnimationManager>();
        }

        protected override void OnUpdate()
        {
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            SystemAPI.SetSingleton(new AttackAnimationManager
            {
                AttackDuration = AttackAnimationManagerConfig.Instance.AnimationDuration,
                AttackAnimationSize = AttackAnimationManagerConfig.Instance.AnimationSize,
                AttackAnimationIdleTime = AttackAnimationManagerConfig.Instance.AnimationIdleTime
            });
        }
    }
}
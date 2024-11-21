using Unity.Entities;

namespace UnitState
{
    public struct SocialDebugManager : IComponentData
    {
        public bool DrawRelations;
    }

    public partial class SocialDebugManagerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            EntityManager.CreateSingleton<SocialDebugManager>();
        }

        protected override void OnUpdate()
        {
            var singleton = SystemAPI.GetSingleton<SocialDebugManager>();
            var config = SocialDebugManagerConfig.Instance;

            singleton.DrawRelations = config.DrawRelations;

            SystemAPI.SetSingleton(singleton);
        }
    }
}
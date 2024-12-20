using Unity.Entities;

namespace UnitState.SocialDebugging
{
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
            singleton.IncludeNonSelections = config.IncludeNonSelections;
            singleton.ApplyFilter = config.ApplyFilter;
            singleton.FilterSetting = config.FilterSetting;
            singleton.ShowEventEffects = config.ShowEventEffects;

            SystemAPI.SetSingleton(singleton);
        }
    }
}
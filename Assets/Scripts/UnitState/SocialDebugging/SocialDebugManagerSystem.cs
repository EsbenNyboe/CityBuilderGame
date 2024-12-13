using System;
using Unity.Entities;

namespace UnitState.SocialDebugging
{
    public struct SocialDebugManager : IComponentData
    {
        public bool DrawRelations;
        public bool IncludeNonSelections;
        public bool ApplyFilter;
        public DrawRelationsFilter FilterSetting;
        public bool ShowEventEffects;
    }

    [Serializable]
    public struct DrawRelationsFilter
    {
        public float MinFondnessDrawn;
        public float MaxFondnessDrawn;
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
            singleton.IncludeNonSelections = config.IncludeNonSelections;
            singleton.ApplyFilter = config.ApplyFilter;
            singleton.FilterSetting = config.FilterSetting;
            singleton.ShowEventEffects = config.ShowEventEffects;

            SystemAPI.SetSingleton(singleton);
        }
    }
}
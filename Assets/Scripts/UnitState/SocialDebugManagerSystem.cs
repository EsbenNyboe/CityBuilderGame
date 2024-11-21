using System;
using Unity.Entities;

namespace UnitState
{
    public struct SocialDebugManager : IComponentData
    {
        public bool DrawRelations;
        public bool ExcludeNonSelections;
        public bool ApplyFilter;
        public DrawRelationsFilter FilterSetting;
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
            singleton.ExcludeNonSelections = config.ExcludeNonSelections;
            singleton.ApplyFilter = config.ApplyFilter;
            singleton.FilterSetting = config.FilterSetting;

            SystemAPI.SetSingleton(singleton);
        }
    }
}
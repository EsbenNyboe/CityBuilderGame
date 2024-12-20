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
}
using UnityEngine;

namespace UnitState
{
    public class SocialDebugManagerConfig : MonoBehaviour
    {
        public static SocialDebugManagerConfig Instance;

        public bool DrawRelations;
        public bool IncludeNonSelections = true;
        public bool ApplyFilter;
        public DrawRelationsFilter FilterSetting;

        public bool DebugBadPerformanceEvents;

        private void Awake()
        {
            Instance = this;
        }
    }
}
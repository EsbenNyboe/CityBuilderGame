using UnityEngine;

namespace UnitState.SocialDebugging
{
    public class SocialDebugManagerConfig : MonoBehaviour
    {
        public static SocialDebugManagerConfig Instance;

        public bool DrawRelations;
        public bool IncludeNonSelections = true;
        public bool ApplyFilter;
        public DrawRelationsFilter FilterSetting;
        public bool ShowEventEffects;

        private void Awake()
        {
            Instance = this;
        }
    }
}
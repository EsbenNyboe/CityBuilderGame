using UnityEngine;

namespace UnitState
{
    public class SocialDebugManagerConfig : MonoBehaviour
    {
        public static SocialDebugManagerConfig Instance;

        public bool DrawRelations;

        private void Awake()
        {
            Instance = this;
        }
    }
}
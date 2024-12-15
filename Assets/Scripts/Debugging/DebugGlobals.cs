using UnityEngine;

namespace Debugging
{
    public class DebugGlobals : MonoBehaviour
    {
        public static DebugGlobals Instance;

        [SerializeField] private bool _showOccupiableGrid;
        [SerializeField] private bool _showWalkableGrid;
        [SerializeField] private bool _showInteractableGrid;

        private void Awake()
        {
            Instance = this;
        }

        public static bool ShowOccupationGrid()
        {
            return Instance._showOccupiableGrid;
        }

        public static bool ShowWalkableGrid()
        {
            return Instance._showWalkableGrid;
        }

        public static bool ShowInteractableGrid()
        {
            return Instance._showInteractableGrid;
        }
    }
}
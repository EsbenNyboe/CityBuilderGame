using UnityEngine;
using UnityEngine.UI;

namespace UnitSpawn.Spawning
{
    public class SpawnSelector : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private SpawnItemType _spawnItemType;

        [SerializeField] private SpawnMenuManager _spawnMenuManager;

        public void SelectSpawnItem(bool isSelected)
        {
            if (isSelected)
            {
                _spawnMenuManager.SetSpawnSelection(_icon.material, _spawnItemType);
            }
            else
            {
                _spawnMenuManager.ResetSpawnSelection();
            }
        }

        public void NotifyOnHoverEvent(bool isHovered)
        {
            _spawnMenuManager.BlockMouseFromSpawning(isHovered);
        }
    }
}
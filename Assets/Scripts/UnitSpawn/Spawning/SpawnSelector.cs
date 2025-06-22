using UnityEngine;
using UnityEngine.UI;

namespace UnitSpawn.Spawning
{
    public class SpawnSelector : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private SpawnItemType _spawnItemType;

        public void SelectSpawnItem(bool isSelected)
        {
            if (isSelected)
            {
                SpawnMenuManager.Instance.SetSpawnSelection(_icon.material, _spawnItemType);
            }
            else
            {
                SpawnMenuManager.Instance.ResetSpawnSelection();
            }
        }

        public void NotifyOnHoverEvent(bool isHovered)
        {
            SpawnMenuManager.Instance.BlockMouseFromSpawning(isHovered);
        }
    }
}
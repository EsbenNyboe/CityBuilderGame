using UnityEngine;
using UnityEngine.UI;

public class SpawnSelector : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private SpawnMenuManager _spawnMenuManager;

    public void SelectSpawnItem(bool isSelected)
    {
        var material = isSelected ? _icon.material : null;
        _spawnMenuManager.SetSpawnSelection(material);
    }
}
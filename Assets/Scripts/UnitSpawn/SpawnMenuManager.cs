using UnityEngine;
using UnityEngine.UI;

public class SpawnMenuManager : MonoBehaviour
{
    public static SpawnMenuManager Instance;
    [SerializeField] private Image _selectionGraphic;
    [SerializeField] private RectTransform _selectorUI;
    private Material _selectionMaterial;
    private SpawnItemType _spawnItemType;
    private bool _spawningIsDisallowed;

    public SpawnItemType ItemToSpawn { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void LateUpdate()
    {
        ItemToSpawn = SpawnItemType.None;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GetComponentInChildren<ToggleGroup>().SetAllTogglesOff();
        }

        var hasSelection = HasSelection();

        if (hasSelection)
        {
            _selectionGraphic.material = _selectionMaterial;
            _selectionGraphic.transform.position = Input.mousePosition;

            if (Input.GetMouseButtonDown(0) && !_spawningIsDisallowed)
            {
                ItemToSpawn = _spawnItemType;
            }
        }

        _selectionGraphic.enabled = hasSelection;
    }

    public bool HasSelection()
    {
        return _selectionMaterial != null;
    }

    public void BlockMouseFromSpawning(bool isBlocked)
    {
        _spawningIsDisallowed = isBlocked;
    }

    public void ResetSpawnSelection()
    {
        _selectionMaterial = null;
        _spawnItemType = SpawnItemType.None;
    }

    public void SetSpawnSelection(Material itemMaterial, SpawnItemType itemType)
    {
        _selectionMaterial = itemMaterial;
        _spawnItemType = itemType;
    }
}

public enum SpawnItemType
{
    None,
    Unit,
    Tree,
    Bed,
    House
}
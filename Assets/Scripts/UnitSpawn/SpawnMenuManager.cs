using UnityEngine;
using UnityEngine.UI;

public class SpawnMenuManager : MonoBehaviour
{
    public static SpawnMenuManager Instance;
    [SerializeField] private Image _selectionGraphic;
    [SerializeField] private RectTransform _selectorUI;
    [SerializeField] private float _minimumTimeBetweenSpawns;
    [SerializeField] private float _doubleClickTiming;
    private Material _selectionMaterial;
    private SpawnItemType _spawnItemType;
    private bool _spawningIsDisallowed;
    private float _timeSinceLastSpawn;
    private float _timeSinceLastClick;
    private bool _isSpawnSpamming;

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

        if (!HasSelection())
        {
            _selectionGraphic.enabled = false;
            return;
        }

        _timeSinceLastClick += Time.deltaTime;
        _timeSinceLastSpawn += Time.deltaTime;

        if (Input.GetMouseButtonDown(0))
        {
            _isSpawnSpamming = _timeSinceLastClick < _doubleClickTiming;
            _timeSinceLastClick = 0;
            TrySpawn();
        }

        if (Input.GetMouseButton(0) && _isSpawnSpamming && !_spawningIsDisallowed)
        {
            TrySpawn();
        }

        _selectionGraphic.material = _selectionMaterial;
        _selectionGraphic.transform.position = Input.mousePosition;
        _selectionGraphic.enabled = true;
    }

    private void TrySpawn()
    {
        if (_timeSinceLastSpawn >= _minimumTimeBetweenSpawns)
        {
            _timeSinceLastSpawn = 0;
            ItemToSpawn = _spawnItemType;
        }
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
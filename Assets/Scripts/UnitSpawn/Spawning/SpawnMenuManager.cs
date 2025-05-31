using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnitSpawn.Spawning
{
    public class SpawnMenuManager : MonoBehaviour
    {
        public static SpawnMenuManager Instance;
        public static bool SpawningIsDisallowed;
        [SerializeField] private Image _selectionGraphic;
        [SerializeField] private RectTransform _selectorUI;
        [SerializeField] private float _minimumTimeBetweenSpawns;
        [SerializeField] private float _minimumTimeBetweenDeletes;
        [SerializeField] private TextMeshProUGUI _brushSizeText;
        private Material _selectionMaterial;
        private SpawnItemType _spawnItemType;
        private float _timeSinceLastSpawn;
        private float _timeSinceLastDelete;
        private bool _autoSpawn;
        private int _brushSize;

        public SpawnItemType ItemToSpawn { get; private set; }
        public SpawnItemType ItemToDelete { get; private set; }

        private void Awake()
        {
            Instance = this;
            SetAutoSpawn(true);
        }

        private void LateUpdate()
        {
            ItemToSpawn = SpawnItemType.None;
            ItemToDelete = SpawnItemType.None;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GetComponentInChildren<ToggleGroup>().SetAllTogglesOff();
            }

            if (!HasSelection())
            {
                _selectionGraphic.enabled = false;
                return;
            }

            _timeSinceLastSpawn += Time.deltaTime;
            _timeSinceLastDelete += Time.deltaTime;

            if (!SpawningIsDisallowed)
            {
                if ((!_autoSpawn && Input.GetMouseButtonDown(0)) ||
                    (_autoSpawn && Input.GetMouseButton(0)))
                {
                    TrySpawn();
                }

                if ((!_autoSpawn && Input.GetMouseButtonDown(1)) || (_autoSpawn && Input.GetMouseButton(1)))
                {
                    TryDelete();
                }
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

        private void TryDelete()
        {
            if (_timeSinceLastDelete > _minimumTimeBetweenDeletes)
            {
                _timeSinceLastDelete = 0;
                ItemToDelete = _spawnItemType;
            }
        }

        public bool HasSelection()
        {
            return _selectionMaterial != null;
        }

        public void BlockMouseFromSpawning(bool isBlocked)
        {
            SpawningIsDisallowed = isBlocked;
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

        public void SetBrushSize(float brushSize)
        {
            _brushSize = Mathf.FloorToInt(brushSize);
            _brushSizeText.text = _brushSize.ToString();
        }

        public int GetBrushSize()
        {
            return _brushSize;
        }

        public void SetAutoSpawn(bool autoSpawn)
        {
            _autoSpawn = autoSpawn;
        }
    }

    public enum SpawnItemType
    {
        None,
        Unit,
        Tree,
        Bed,
        Storage,
        Boar,
        House
    }
}
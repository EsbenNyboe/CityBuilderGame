using System;
using UnityEngine;
using UnityEngine.UI;

public class SpawnMenuManager : MonoBehaviour
{
    [SerializeField] private Image _selectionGraphic;
    [SerializeField] private RectTransform _selectorUI;
    private Material _selectionMaterial;
    private SpawnItemType _spawnItemType;

    private bool _spawningIsDisallowed;

    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GetComponentInChildren<ToggleGroup>().SetAllTogglesOff();
        }

        var hasSelection = _selectionMaterial != null;

        if (hasSelection)
        {
            _selectionGraphic.material = _selectionMaterial;
            _selectionGraphic.transform.position = Input.mousePosition;

            if (Input.GetMouseButtonDown(0) && !_spawningIsDisallowed)
            {
                TrySpawnSelectedItem();
            }
        }

        _selectionGraphic.enabled = hasSelection;
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

    private void TrySpawnSelectedItem()
    {
        switch (_spawnItemType)
        {
            case SpawnItemType.None:
                break;
            case SpawnItemType.Unit:
                Debug.Log("Spawn unit");
                break;
            case SpawnItemType.Tree:
                Debug.Log("Spawn tree");
                break;
            case SpawnItemType.Bed:
                Debug.Log("Spawn bed");
                break;
            case SpawnItemType.House:
                Debug.Log("Spawn house");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
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
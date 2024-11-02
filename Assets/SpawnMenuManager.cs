using UnityEngine;
using UnityEngine.UI;

public class SpawnMenuManager : MonoBehaviour
{
    [SerializeField] private Image _selectionGraphic;
    private Material _selectionMaterial;

    private void Update()
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
        }

        _selectionGraphic.enabled = hasSelection;
    }

    public void SetSpawnSelection(Material itemMaterial)
    {
        _selectionMaterial = itemMaterial;
    }
}
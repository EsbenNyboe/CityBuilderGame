using Grid.SaveLoad;
using UnityEngine;

public class SaveLoadMenu : MonoBehaviour
{
    private void OnEnable()
    {
        SavedGridStateManager.Instance.SetMenuVisibility();
    }
}
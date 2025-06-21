using Grid.SaveLoad;
using UnityEngine;

public class OnEnableEvent : MonoBehaviour
{
    private void OnEnable()
    {
        SavedGridStateManager.Instance.SetMenuVisibility();
    }
}
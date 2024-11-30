using Grid.SaveLoad;
using UnityEngine;

public class SaveSlotUI : MonoBehaviour
{
    public void Save()
    {
        GetComponentInParent<SavedGridStateManager>().SaveToSlot(transform);
    }

    public void Load()
    {
        GetComponentInParent<SavedGridStateManager>().LoadFromSlot(transform);
    }

    public void Delete()
    {
        GetComponentInParent<SavedGridStateManager>().DeleteDataInSlot(transform);
    }
}
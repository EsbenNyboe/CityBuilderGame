using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Grid.SaveLoad
{
    public class SavedGridStateManager : MonoBehaviour
    {
        public static SavedGridStateManager Instance;
        [SerializeField] private SavedGridStateObject _saveSlot;

        private void Awake()
        {
            Instance = this;
        }

        public void SaveDataToSaveSlot(int2[] trees, int2[] beds, int2[] dropPoints)
        {
            _saveSlot.Trees = trees;
            _saveSlot.Beds = beds;
            _saveSlot.DropPoints = dropPoints;
#if UNITY_EDITOR
            EditorUtility.SetDirty(_saveSlot);
#endif
        }

        public int2[] LoadSavedTrees()
        {
            return _saveSlot.Trees;
        }

        public int2[] LoadSavedBeds()
        {
            return _saveSlot.Beds;
        }

        public int2[] LoadSavedDropPoints()
        {
            return _saveSlot.DropPoints;
        }
    }
}
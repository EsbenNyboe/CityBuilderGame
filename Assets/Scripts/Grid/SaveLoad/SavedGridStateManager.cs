using System;
using TMPro;
using Unity.Assertions;
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
        [SerializeField] private GameObject _saveSlotUIPrefab;

        [SerializeField] private SavedGridStateObject[] _saveSlots;
        [SerializeField] private Transform _saveSlotTransformParent;

        [HideInInspector] public int SlotToSave = -1;
        [HideInInspector] public int SlotToLoad = -1;
        [HideInInspector] public int SlotToDelete = -1;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            for (var i = 0; i < _saveSlots.Length; i++)
            {
                var saveSlotUI = Instantiate(_saveSlotUIPrefab, _saveSlotTransformParent);
                saveSlotUI.GetComponentInChildren<TextMeshProUGUI>().text = "SaveSlot " + i;
            }
        }

        public void SaveDataToSaveSlot(int2[] trees, int2[] beds, int2[] dropPoints)
        {
            Assert.IsTrue(SlotToSave > -1 && SlotToSave < _saveSlots.Length);

            _saveSlots[SlotToSave].Trees = trees;
            _saveSlots[SlotToSave].Beds = beds;
            _saveSlots[SlotToSave].DropPoints = dropPoints;
#if UNITY_EDITOR
            EditorUtility.SetDirty(_saveSlots[SlotToSave]);
#endif
        }

        public void DeleteDataOnSaveSlot()
        {
            Assert.IsTrue(SlotToDelete > -1 && SlotToDelete < _saveSlots.Length);

            _saveSlots[SlotToDelete].Trees = Array.Empty<int2>();
            _saveSlots[SlotToDelete].Beds = Array.Empty<int2>();
            _saveSlots[SlotToDelete].DropPoints = Array.Empty<int2>();
#if UNITY_EDITOR
            EditorUtility.SetDirty(_saveSlots[SlotToDelete]);
#endif
        }

        public int2[] LoadSavedTrees()
        {
            Assert.IsTrue(SlotToLoad > -1 && SlotToLoad < _saveSlots.Length);
            return _saveSlots[SlotToLoad].Trees;
        }

        public int2[] LoadSavedBeds()
        {
            Assert.IsTrue(SlotToLoad > -1 && SlotToLoad < _saveSlots.Length);
            return _saveSlots[SlotToLoad].Beds;
        }

        public int2[] LoadSavedDropPoints()
        {
            Assert.IsTrue(SlotToLoad > -1 && SlotToLoad < _saveSlots.Length);
            return _saveSlots[SlotToLoad].DropPoints;
        }

        public void SaveToSlot(Transform slotTransform)
        {
            RegisterSlotProcedure(slotTransform, ref SlotToSave);
        }

        public void LoadFromSlot(Transform slotTransform)
        {
            RegisterSlotProcedure(slotTransform, ref SlotToLoad);
        }

        public void DeleteDataInSlot(Transform slotTransform)
        {
            RegisterSlotProcedure(slotTransform, ref SlotToDelete);
        }

        private void RegisterSlotProcedure(Transform slotTransform, ref int slotProcedure)
        {
            UnityEngine.Assertions.Assert.IsTrue(slotTransform.IsChildOf(_saveSlotTransformParent));
            UnityEngine.Assertions.Assert.IsTrue(_saveSlots.Length == _saveSlotTransformParent.childCount);

            for (var i = 0; i < _saveSlotTransformParent.childCount; i++)
            {
                var child = _saveSlotTransformParent.GetChild(i);
                if (slotTransform == child)
                {
                    slotProcedure = i;
                }
            }
        }
    }
}
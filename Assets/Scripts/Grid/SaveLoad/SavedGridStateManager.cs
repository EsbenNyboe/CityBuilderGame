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
        private SaveSlotUI[] _saveSlotUIs;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            for (var i = 0; i < _saveSlotTransformParent.childCount; i++)
            {
                Destroy(_saveSlotTransformParent.GetChild(i).gameObject);
            }

            _saveSlotUIs = new SaveSlotUI[_saveSlots.Length];
            for (var i = 0; i < _saveSlots.Length; i++)
            {
                var saveSlotUI = Instantiate(_saveSlotUIPrefab, _saveSlotTransformParent);
                saveSlotUI.GetComponentInChildren<TextMeshProUGUI>().text = "SaveSlot " + i;
                _saveSlotUIs[i] = saveSlotUI.GetComponent<SaveSlotUI>();
                _saveSlotUIs[i].Initialize(_saveSlots[i]);
            }
        }

        public void SaveDataToSaveSlot(int2[] trees, int2[] beds, int2[] dropPoints)
        {
            Assert.IsTrue(SlotToSave > -1 && SlotToSave < _saveSlots.Length);

            var saveSlot = _saveSlots[SlotToSave];
            saveSlot.Trees = trees;
            saveSlot.Beds = beds;
            saveSlot.DropPoints = dropPoints;
#if UNITY_EDITOR
            EditorUtility.SetDirty(saveSlot);
#endif
            _saveSlotUIs[SlotToSave].Initialize(saveSlot);
        }

        public void DeleteDataOnSaveSlot()
        {
            Assert.IsTrue(SlotToDelete > -1 && SlotToDelete < _saveSlots.Length);

            var saveSlot = _saveSlots[SlotToDelete];
            saveSlot.Trees = Array.Empty<int2>();
            saveSlot.Beds = Array.Empty<int2>();
            saveSlot.DropPoints = Array.Empty<int2>();
#if UNITY_EDITOR
            EditorUtility.SetDirty(saveSlot);
#endif
            _saveSlotUIs[SlotToDelete].Initialize(saveSlot);
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
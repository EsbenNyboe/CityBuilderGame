using System;
using System.Collections;
using TMPro;
using Unity.Assertions;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Grid.SaveLoad
{
    public class SavedGridStateManager : MonoBehaviour
    {
        public static SavedGridStateManager Instance;
        [SerializeField] private GameObject _saveSlotUIPrefab;

        [SerializeField] private SavedGridStateObject[] _saveSlots;
        [SerializeField] private Transform _saveSlotTransformParent;

        [HideInInspector] public int SlotToSave = -1;
        [HideInInspector] public int SlotToLoad = -1;
        [HideInInspector] public int SlotToDelete = -1;
        private SaveSlotUI[] _saveSlotUIs;
        private Coroutine[] _saveSlotUpdateCoroutines;
        private bool _showSaveMenu;

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
            _saveSlotUpdateCoroutines = new Coroutine[_saveSlots.Length];
            for (var i = 0; i < _saveSlots.Length; i++)
            {
                var saveSlotUI = Instantiate(_saveSlotUIPrefab, _saveSlotTransformParent);
                saveSlotUI.GetComponentInChildren<TextMeshProUGUI>().text = "SaveSlot " + (i + 1);
                _saveSlotUIs[i] = saveSlotUI.GetComponent<SaveSlotUI>();
            }

            SetMenuVisibility();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _showSaveMenu = !_showSaveMenu;
                SetMenuVisibility();
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
            UpdateSaveSlot(SlotToSave);
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
            UpdateSaveSlot(SlotToDelete);
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

        public void OnLoaded()
        {
            UpdateSaveSlot(SlotToLoad);
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

        private void SetMenuVisibility()
        {
            _saveSlotTransformParent.gameObject.SetActive(_showSaveMenu);
            for (var i = 0; i < _saveSlots.Length; i++)
            {
                UpdateSaveSlot(i);
            }
        }

        private void UpdateSaveSlot(int i)
        {
            if (_saveSlotUpdateCoroutines[i] != null)
            {
                StopCoroutine(_saveSlotUpdateCoroutines[i]);
            }

            _saveSlotUpdateCoroutines[i] = StartCoroutine(DelayedSetActive(i));
        }

        private IEnumerator DelayedSetActive(int i)
        {
            _saveSlotUIs[i].GetComponentInChildren<Image>(true).gameObject.SetActive(false);
            var waitedFrames = 0;
            while (waitedFrames < i)
            {
                yield return new WaitForEndOfFrame();
                waitedFrames++;
            }

            _saveSlotUIs[i].GetComponentInChildren<Image>(true).gameObject.SetActive(true);
            _saveSlotUIs[i].Initialize(_saveSlots[i]);
        }
    }
}
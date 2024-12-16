using UnityEngine;

namespace UnitControl
{
    public class SelectionAreaManager : MonoBehaviour
    {
        public static SelectionAreaManager Instance;
        public Transform SelectionArea;
        public Material UnitSelectedMaterial;

        [HideInInspector] public Mesh UnitSelectedMesh;
        [SerializeField] private float _meshWidth;
        [SerializeField] private float _meshHeight;
        [SerializeField] private float _minSelectionArea = 2f;

        private void Awake()
        {
            Instance = this;

            UnitSelectedMesh = ECS_Animation.CreateMesh(_meshWidth, _meshHeight);
        }

        public float GetMinSelectionArea()
        {
            return _minSelectionArea;
        }
    }
}
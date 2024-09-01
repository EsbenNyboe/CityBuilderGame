using ECS_AnimationSystem;
using UnityEngine;

public class SelectionAreaManager : MonoBehaviour
{
    public static SelectionAreaManager Instance;
    public Transform SelectionArea;
    public Material UnitSelectedMaterial;

    [HideInInspector] public Mesh UnitSelectedMesh;
    [SerializeField] private float _meshWidth;
    [SerializeField] private float _meshHeight;

    private void Awake()
    {
        Instance = this;

        UnitSelectedMesh = ECS_Animation.CreateMesh(_meshWidth, _meshHeight);
    }
}
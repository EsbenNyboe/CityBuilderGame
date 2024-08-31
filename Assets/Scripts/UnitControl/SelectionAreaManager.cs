using ECS_AnimationSystem;
using UnityEngine;

public class SelectionAreaManager : MonoBehaviour
{
    public static SelectionAreaManager Instance;
    public Transform SelectionArea;
    public Material UnitSelectedMaterial;

    [HideInInspector] public Mesh UnitSelectedMesh;

    private void Awake()
    {
        Instance = this;

        UnitSelectedMesh = ECS_Animation.CreateMesh(5f, 10f);
    }
}
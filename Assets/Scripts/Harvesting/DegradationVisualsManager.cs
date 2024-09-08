using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Rendering;
using Unity.VisualScripting;
using UnityEngine;

public class DegradationVisualsManager : MonoBehaviour
{
    [SerializeField]
    private Material[] _treeMaterials;

    public static DegradationVisualsManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public Material[] GetMaterials()
    {
        return _treeMaterials;
    }
}
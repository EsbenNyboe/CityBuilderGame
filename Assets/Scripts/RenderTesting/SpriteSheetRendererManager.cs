using System;
using UnityEngine;

public class SpriteSheetRendererManager : MonoBehaviour
{
    public static SpriteSheetRendererManager Instance;
    public Mesh TestMesh; 
    public Material TestMaterial;

    private void Awake()
    {
        Instance = this;
    }
}
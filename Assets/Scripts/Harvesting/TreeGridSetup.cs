using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TreeGridSetup : MonoBehaviour
{
    [SerializeField] private AreaToExclude[] _areasToExclude;

    public static TreeGridSetup Instance;

    private void Awake()
    {
        Instance = this;
    }

    public static AreaToExclude[] AreasToExclude()
    {
        return Instance._areasToExclude;
    }

    [Serializable]
    public class AreaToExclude
    {
        public int2 StartCell;
        public int2 EndCell;
    }
}
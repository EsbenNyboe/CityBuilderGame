using System;
using Unity.Mathematics;
using UnityEngine;

namespace Grid.SaveLoad
{
    [CreateAssetMenu(menuName = "SaveLoad/Saved Grid State Object")]
    public class SavedGridStateObject : ScriptableObject
    {
        public int2 GridSize;
        public int2[] Trees = Array.Empty<int2>();
        public int2[] Beds = Array.Empty<int2>();
        public int2[] Storages = Array.Empty<int2>();
        public int2[] Bonfires = Array.Empty<int2>();
        public int2[] Houses = Array.Empty<int2>();
        public float3[] Villagers = Array.Empty<float3>();
        public float3[] Boars = Array.Empty<float3>();
    }
}
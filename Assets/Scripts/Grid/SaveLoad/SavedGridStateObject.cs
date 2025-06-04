using Unity.Mathematics;
using UnityEngine;

namespace Grid.SaveLoad
{
    [CreateAssetMenu(menuName = "SaveLoad/Saved Grid State Object")]
    public class SavedGridStateObject : ScriptableObject
    {
        public int2 GridSize;
        public int2[] Trees;
        public int2[] Beds;
        public int2[] Storages;
        public int2[] Bonfires;
        public int2[] Houses;
        public float3[] Villagers;
        public float3[] Boars;
    }
}
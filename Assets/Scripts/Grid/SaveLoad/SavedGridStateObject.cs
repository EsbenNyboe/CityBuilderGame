using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Grid.SaveLoad
{
    [CreateAssetMenu(menuName = "SaveLoad/Saved Grid State Object")]
    public class SavedGridStateObject : ScriptableObject
    {
        public int2 GridSize;
        public int2[] Trees;
        public int2[] Beds;
        [FormerlySerializedAs("DropPoints")] public int2[] Storages;
    }
}
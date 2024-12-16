using System;
using Unity.Mathematics;
using UnityEngine;

namespace Grid.Setup
{
    public class TreeGridSetup : MonoBehaviour
    {
        public static TreeGridSetup Instance;
        [SerializeField] private AreaToExclude[] _areasToExclude;

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
}
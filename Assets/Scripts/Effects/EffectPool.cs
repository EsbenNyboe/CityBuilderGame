using System.Collections.Generic;
using UnityEngine;

namespace Effects
{
    public abstract class EffectPool<T> where T : Component
    {
        public GameObject Prefab;
        public Queue<T> Pool;

        // public T CreatePoolItem(GameObject prefab, Transform transform)
        // {
        // }

        public abstract bool IsAvailable();
    }
}
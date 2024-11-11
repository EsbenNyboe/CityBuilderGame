using System.Collections.Generic;
using UnityEngine;

namespace Rendering
{
    public class DeathAnimationManager : MonoBehaviour
    {
        public static DeathAnimationManager Instance;
        [SerializeField] private GameObject _deathAnimationPrefab;

        private Queue<ParticleSystem> _pool;

        private void Awake()
        {
            Instance = this;

            _pool = new Queue<ParticleSystem>();
            var newPoolItem = CreatePoolItem();
            _pool.Enqueue(newPoolItem);
        }

        private ParticleSystem CreatePoolItem()
        {
            return Instantiate(_deathAnimationPrefab, Vector3.zero, Quaternion.identity).GetComponent<ParticleSystem>();
        }

        public void PlayDeathAnimation(Vector3 position)
        {
            ParticleSystem poolItem;
            if (_pool.TryPeek(out var nextItem) && !nextItem.isPlaying)
            {
                poolItem = _pool.Dequeue();
            }
            else
            {
                poolItem = CreatePoolItem();
            }

            poolItem.transform.position = position;
            poolItem.Play();
            _pool.Enqueue(poolItem);
        }
    }
}
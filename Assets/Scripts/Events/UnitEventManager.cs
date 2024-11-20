using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Events
{
    public class UnitEventManager : MonoBehaviour
    {
        public static UnitEventManager Instance;
        [SerializeField] private GameObject _deathEffectPrefab;
        [SerializeField] private GameObject _damageEffectPrefab;

        private Queue<ParticleSystem> _deathEffectPool;
        private Queue<ParticleSystem> _damageEffectPool;

        private void Awake()
        {
            Instance = this;

            _deathEffectPool = new Queue<ParticleSystem>();
            _deathEffectPool.Enqueue(CreatePoolItem(_deathEffectPrefab));
            _damageEffectPool = new Queue<ParticleSystem>();
            _damageEffectPool.Enqueue(CreatePoolItem(_damageEffectPrefab));
        }

        private ParticleSystem CreatePoolItem(GameObject prefab)
        {
            return Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<ParticleSystem>();
        }

        public void PlayDeathEffect(Vector3 position)
        {
            PlayParticleEffect(_deathEffectPool, _deathEffectPrefab, position);
        }

        public void PlayDamageEffect(float3 position)
        {
            PlayParticleEffect(_damageEffectPool, _damageEffectPrefab, position);
        }

        private void PlayParticleEffect(Queue<ParticleSystem> pool, GameObject prefab, Vector3 position)
        {
            ParticleSystem poolItem;
            if (pool.TryPeek(out var nextItem) && !nextItem.isPlaying)
            {
                poolItem = pool.Dequeue();
            }
            else
            {
                poolItem = CreatePoolItem(prefab);
            }

            poolItem.transform.position = position;
            poolItem.Play();
            pool.Enqueue(poolItem);
        }
    }
}
using Unity.Mathematics;
using UnityEngine;

namespace Events
{
    public class ParticleEffectManager : MonoBehaviour
    {
        public static ParticleEffectManager Instance;
        [SerializeField] private PoolManager<ParticleSystem> _deathEffectPoolManager;
        [SerializeField] private PoolManager<ParticleSystem> _damageEffectPoolManager;

        private void Awake()
        {
            Instance = this;
        }

        public void PlayDeathEffect(Vector3 position)
        {
            PlayParticleEffect(_deathEffectPoolManager, position);
        }

        public void PlayDamageEffect(float3 position)
        {
            PlayParticleEffect(_damageEffectPoolManager, position);
        }

        private void PlayParticleEffect(PoolManager<ParticleSystem> poolManager, Vector3 position)
        {
            var poolItem = poolManager.GetOrCreatePoolItem();
            poolManager.EnqueuePoolItem(poolItem, position);
        }
    }
}
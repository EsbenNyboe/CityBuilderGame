using UnityEngine;

namespace Effects
{
    public class SpriteEffectManager : MonoBehaviour
    {
        public static SpriteEffectManager Instance;

        [SerializeField] private TimedImagePoolManager[] _damageEffectPools;
        [Range(0, 0.5f)] [SerializeField] private float _randomPositionFactor;

        private int _currentSelection;

        private void Awake()
        {
            Instance = this;
        }

        public void PlayDamageEffect(Vector3 position)
        {
            _currentSelection++;
            if (_currentSelection >= _damageEffectPools.Length)
            {
                _currentSelection = 0;
            }

            var poolItem = _damageEffectPools[_currentSelection].GetOrCreatePoolItem();
            position.x += Random.Range(-_randomPositionFactor, _randomPositionFactor);
            position.y += Random.Range(-_randomPositionFactor, _randomPositionFactor);
            _damageEffectPools[_currentSelection].EnqueuePoolItem(poolItem, position);
        }
    }
}
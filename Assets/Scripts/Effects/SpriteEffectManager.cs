using UnityEngine;

public class SpriteEffectManager : MonoBehaviour
{
    public static SpriteEffectManager Instance;

    [SerializeField] private TimedImagePoolManager[] _damageEffectPools;
    [Range(0, 0.5f)] [SerializeField] private float _randomPositionFactor;

    [SerializeField] private TimedImagePoolManager _socialPlusEffectPool;
    [SerializeField] private TimedImagePoolManager _socialMinusEffectPool;
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

    public void PlaySocialPlusEffect(Vector3 position)
    {
        var poolItem = _socialPlusEffectPool.GetOrCreatePoolItem();
        _socialPlusEffectPool.EnqueuePoolItem(poolItem, position);
    }

    public void PlaySocialMinusEffect(Vector3 position)
    {
        var poolItem = _socialMinusEffectPool.GetOrCreatePoolItem();
        _socialMinusEffectPool.EnqueuePoolItem(poolItem, position);
    }
}
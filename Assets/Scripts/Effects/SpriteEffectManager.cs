using UnityEngine;

public class SpriteEffectManager : MonoBehaviour
{
    public static SpriteEffectManager Instance;

    [SerializeField] private TimedImagePoolManager[] _damageEffectPools;
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
        _damageEffectPools[_currentSelection].EnqueuePoolItem(poolItem, position);
    }
}
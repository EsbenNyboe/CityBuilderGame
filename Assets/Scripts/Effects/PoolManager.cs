using System.Collections.Generic;
using UnityEngine;

public abstract class PoolManager<T> : MonoBehaviour where T : Component
{
    [SerializeField] private GameObject _poolItemTemplate;
    [Min(1)] [SerializeField] private int _preferredPoolSize;
    [Min(0)] [SerializeField] private float _poolCleanupInterval;
    private Queue<T> _pool;
    private float _timeOfLatestPoolCleanup;

    private void Awake()
    {
        _pool = new Queue<T>();
    }

    private void Update()
    {
        if (_timeOfLatestPoolCleanup + _poolCleanupInterval > Time.time)
        {
            return;
        }

        _timeOfLatestPoolCleanup = Time.time;
        var poolSizeBeforeCleanup = _pool.Count;
        var numOfItemsToDestroy =
            poolSizeBeforeCleanup <= _preferredPoolSize ? 0 : poolSizeBeforeCleanup - _preferredPoolSize;
        var numOfItemsToCleanup = poolSizeBeforeCleanup;

        if (numOfItemsToDestroy > 0)
        {
            Debug.LogWarning("Pool exceeded the preferred size with " +
                             numOfItemsToDestroy + " pool items. " +
                             "Consider allocating more space for the pool", gameObject);
        }

        while (numOfItemsToCleanup > 0)
        {
            numOfItemsToCleanup--;
            var poolItem = _pool.Dequeue();
            if (IsActive(poolItem))
            {
                _pool.Enqueue(poolItem);
                continue;
            }

            if (numOfItemsToDestroy <= 0)
            {
                poolItem.transform.position = Vector3.zero;
                _pool.Enqueue(poolItem);
            }
            else
            {
                numOfItemsToDestroy--;
                Destroy(poolItem.gameObject);
            }
        }

        if (numOfItemsToDestroy > 0)
        {
            Debug.LogError("Pool cleanup failed. Items that were not destroyed: " + numOfItemsToDestroy);
        }
    }

    public T GetOrCreatePoolItem()
    {
        if (!TryDequeuePoolItem(out var poolItem))
        {
            poolItem = CreatePoolItem();
        }

        return poolItem;
    }

    public void EnqueuePoolItem(T poolItem, Vector3 position)
    {
        poolItem.transform.position = position;
        Play(poolItem);
        _pool.Enqueue(poolItem);
    }

    private bool TryDequeuePoolItem(out T poolItem)
    {
        if (!_pool.TryPeek(out poolItem) || IsActive(poolItem))
        {
            return false;
        }

        poolItem = _pool.Dequeue();
        return true;
    }

    private T CreatePoolItem()
    {
        var poolObject = Instantiate(_poolItemTemplate, transform);
        var poolItem = poolObject.GetComponent<T>();
        return poolItem;
    }

    protected abstract bool IsActive(T poolItem);

    protected abstract void Play(T poolItem);
}
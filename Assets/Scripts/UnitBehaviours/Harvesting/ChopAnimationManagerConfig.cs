using UnityEngine;

public class ChopAnimationManagerConfig : MonoBehaviour
{
    public static ChopAnimationManagerConfig Instance;

    [SerializeField] private float _chopDuration = 1f;

    [SerializeField] private float _damagePerChop = 10f;

    [SerializeField] private float _chopAnimationSize = 0.5f;

    [SerializeField] [Range(0f, 0.999f)] private float _chopAnimationIdleTime = 0.1f;

    private void Awake()
    {
        Instance = this;
    }

    public static float ChopDuration()
    {
        return Instance._chopDuration / Globals.GameSpeed();
    }

    public static float DamagePerChop()
    {
        return Instance._damagePerChop;
    }

    public static float ChopAnimationSize()
    {
        return Instance._chopAnimationSize;
    }

    public static float ChopAnimationPostIdleTimeNormalized()
    {
        return Instance._chopAnimationIdleTime;
    }
}
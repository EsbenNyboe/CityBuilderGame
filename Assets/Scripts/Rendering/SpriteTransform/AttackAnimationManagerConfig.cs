using UnityEngine;

public class AttackAnimationManagerConfig : MonoBehaviour
{
    public static AttackAnimationManagerConfig Instance;

    [SerializeField] private float _chopDuration = 1f;

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

    public static float ChopAnimationSize()
    {
        return Instance._chopAnimationSize;
    }

    public static float ChopAnimationPostIdleTimeNormalized()
    {
        return Instance._chopAnimationIdleTime;
    }
}
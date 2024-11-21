using UnityEngine;

public class ParticleSystemPoolManager : PoolManager<ParticleSystem>
{
    protected override bool IsActive(ParticleSystem poolItem)
    {
        return poolItem.isPlaying;
    }

    protected override void Play(ParticleSystem poolItem)
    {
        poolItem.Play();
    }
}
using UnityEngine;

public class AudioSourcePoolManager : PoolManager<AudioSource>
{
    protected override bool IsActive(AudioSource poolItem)
    {
        return poolItem.isPlaying;
    }

    protected override void Play(AudioSource poolItem)
    {
        poolItem.Play();
    }
}
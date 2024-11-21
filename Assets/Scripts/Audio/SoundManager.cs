using System;
using System.Collections.Generic;
using Audio;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource _template;

    [SerializeField] private SoundConfig _chopSound;
    [SerializeField] private SoundConfig _destroyTreeSound;
    [SerializeField] private SoundConfig _dieSound;
    [SerializeField] private MultiSoundConfigObject _damageSound;

    [SerializeField] private SoundConfigObject _previewSound;

    [Min(1)] [SerializeField] private int _preferredPoolSize;

    [Min(0)] [SerializeField] private float _poolCleanupInterval;

    private Queue<AudioSource> _pool;
    private float _timeOfLatestPoolCleanup;

    public static SoundManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        _pool = new Queue<AudioSource>();
        var newPoolItem = CreatePoolItem();
        InitializePoolItem(ref newPoolItem, _template);
        _pool.Enqueue(newPoolItem);
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
        while (numOfItemsToCleanup > 0)
        {
            numOfItemsToCleanup--;
            var poolItem = _pool.Dequeue();
            if (poolItem.isPlaying)
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
            Debug.LogError("Pool size is too small. It's preferred size is " + _preferredPoolSize +
                           ", but its required size is " + _pool.Count +
                           ". Overflow: " + numOfItemsToDestroy);
        }
    }

    public void PlayPreviewSound()
    {
        if (_previewSound is MultiSoundConfigObject)
        {
            PlayAtPosition(_previewSound as MultiSoundConfigObject, Vector3.zero);
        }
    }

    public void PlayChopSound(Vector3 position)
    {
        PlayAtPosition(_chopSound, position);
    }

    public void PlayDestroyTreeSound(Vector3 position)
    {
        PlayAtPosition(_destroyTreeSound, position);
    }

    public void PlayDeathSound(Vector3 position)
    {
        PlayAtPosition(_dieSound, position);
    }

    public void PlayDamageSound(float3 position)
    {
        PlayAtPosition(_damageSound, position);
    }

    private void PlayAtPosition(MultiSoundConfigObject sound, Vector3 position)
    {
        var clipSelection = Random.Range(0, sound.Clips.Length);
        PlayAtPosition(sound.Clips[clipSelection], sound.Volume, sound.PitchCenter, sound.PitchVariance, position);
    }

    private void PlayAtPosition(SoundConfig sound, Vector3 position)
    {
        PlayAtPosition(sound.Clip, sound.Volume, sound.PitchCenter, sound.PitchVariance, position);
    }

    private void PlayAtPosition(AudioClip clip, float volume, float pitchCenter, float pitchVariance, Vector3 position)
    {
        AudioSource poolItem;
        if (_pool.TryPeek(out var nextItem) && !nextItem.isPlaying)
        {
            poolItem = _pool.Dequeue();
        }
        else
        {
            poolItem = CreatePoolItem();
            InitializePoolItem(ref poolItem, _template);
        }

        ApplySoundConfig(ref poolItem, clip, volume, pitchCenter, pitchVariance);
        ApplyDebugInfo(ref poolItem, clip);

        poolItem.transform.position = position;
        poolItem.Play();
        _pool.Enqueue(poolItem);
    }

    private void ApplyDebugInfo(ref AudioSource poolItem, AudioClip clip)
    {
        // poolItem.gameObject.name = Time.time + ": " + sound.Clip.name;
    }

    private static void ApplySoundConfig(ref AudioSource poolItem, AudioClip clip, float volume, float pitchCenter,
        float pitchVariance)
    {
        poolItem.clip = clip;
        poolItem.volume = volume;
        var pitchMin = pitchCenter - pitchVariance;
        var pitchMax = pitchCenter + pitchVariance;
        poolItem.pitch = Random.Range(pitchMin, pitchMax);
    }

    private AudioSource CreatePoolItem()
    {
        var audioSourceObject = new GameObject();
        audioSourceObject.transform.SetParent(transform);
        var audioSource = audioSourceObject.AddComponent<AudioSource>();
        return audioSource;
    }

    private void InitializePoolItem(ref AudioSource poolItem, AudioSource template)
    {
        poolItem.dopplerLevel = template.dopplerLevel;
        poolItem.bypassReverbZones = template.bypassReverbZones;
        poolItem.clip = template.clip;
        poolItem.ignoreListenerPause = template.ignoreListenerPause;
        poolItem.ignoreListenerVolume = template.ignoreListenerVolume;
        poolItem.loop = template.loop;
        poolItem.maxDistance = template.maxDistance;
        poolItem.minDistance = template.minDistance;
        poolItem.mute = template.mute;
        poolItem.outputAudioMixerGroup = template.outputAudioMixerGroup;
        poolItem.panStereo = template.panStereo;
        poolItem.pitch = template.pitch;
        poolItem.playOnAwake = template.playOnAwake;
        poolItem.priority = template.priority;
        poolItem.reverbZoneMix = template.reverbZoneMix;
        poolItem.rolloffMode = template.rolloffMode;
        poolItem.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
            template.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
        poolItem.spatialBlend = template.spatialBlend;
        poolItem.spatialize = template.spatialize;
        poolItem.spatializePostEffects = template.spatializePostEffects;
        poolItem.spread = template.spread;
        poolItem.velocityUpdateMode = template.velocityUpdateMode;
        poolItem.volume = template.volume;
    }

    [Serializable]
    public class SoundConfig
    {
        public AudioClip Clip;
        [Range(0, 1)] public float Volume = 1;
        [Range(0.5f, 1.5f)] public float PitchCenter = 1;
        [Range(0f, 0.499f)] public float PitchVariance;
    }
}
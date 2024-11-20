using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource _template;

    [SerializeField] private SoundConfig _chopSound;
    [SerializeField] private SoundConfig _destroyTreeSound;
    [SerializeField] private SoundConfig _dieSound;
    [SerializeField] private SoundConfig _damageSound;

    private Queue<AudioSource> _pool;

    public static SoundManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        _pool = new Queue<AudioSource>();
        var newPoolItem = CreatePoolItem();
        InitializePoolItem(ref newPoolItem, _template);
        _pool.Enqueue(newPoolItem);
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

    private void PlayAtPosition(SoundConfig sound, Vector3 position)
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

        ApplySoundConfig(ref poolItem, sound);
        ApplyDebugInfo(ref poolItem, sound);

        poolItem.transform.position = position;
        poolItem.Play();
        _pool.Enqueue(poolItem);
    }

    private void ApplyDebugInfo(ref AudioSource poolItem, SoundConfig sound)
    {
        // poolItem.gameObject.name = Time.time + ": " + sound.Clip.name;
    }

    private static void ApplySoundConfig(ref AudioSource poolItem, SoundConfig sound)
    {
        poolItem.clip = sound.Clip;
        poolItem.volume = sound.Volume;
        var pitchMin = sound.PitchCenter - sound.PitchVariance;
        var pitchMax = sound.PitchCenter + sound.PitchVariance;
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
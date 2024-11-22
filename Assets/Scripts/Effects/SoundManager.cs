﻿using System;
using Audio;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private SoundConfig _chopSound;
    [SerializeField] private SoundConfig _destroyTreeSound;
    [SerializeField] private SoundConfig _dieSound;
    [SerializeField] private MultiSoundConfigObject _damageSound;

    [SerializeField] private SoundConfigObject _previewSound;

    [SerializeField] private AudioSourcePoolManager _defaultPoolManager;
    public static SoundManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
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
        var poolItem = _defaultPoolManager.GetOrCreatePoolItem();

        ApplySoundConfig(ref poolItem, clip, volume, pitchCenter, pitchVariance);
        ApplyDebugInfo(ref poolItem, clip);

        _defaultPoolManager.EnqueuePoolItem(poolItem, position);
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

    [Serializable]
    public class SoundConfig
    {
        public AudioClip Clip;
        [Range(0, 1)] public float Volume = 1;
        [Range(0.5f, 1.5f)] public float PitchCenter = 1;
        [Range(0f, 0.499f)] public float PitchVariance;
    }
}
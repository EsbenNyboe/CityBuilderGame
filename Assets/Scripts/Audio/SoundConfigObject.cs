using UnityEngine;

namespace Audio
{
    public class SoundConfigObject : ScriptableObject
    {
        [Range(0, 1)] public float Volume = 1;
        [Range(0.5f, 1.5f)] public float PitchCenter = 1;
        [Range(0f, 0.499f)] public float PitchVariance;
    }
}
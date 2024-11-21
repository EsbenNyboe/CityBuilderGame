using UnityEngine;

namespace Audio
{
    [CreateAssetMenu(menuName = "Audio/Multi Sound Config")]
    public class MultiSoundConfigObject : SoundConfigObject
    {
        public AudioClip[] Clips;
    }
}
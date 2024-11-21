using UnityEngine;

namespace Audio
{
    [CreateAssetMenu(menuName = "Audio/Single Sound Config")]
    public class SingleSoundConfigObject : SoundConfigObject
    {
        public AudioClip Clip;
    }
}
using UnityEngine;

namespace Rendering
{
    public class DeathAnimationManager : MonoBehaviour
    {
        public static DeathAnimationManager Instance;
        [SerializeField] private GameObject _deathAnimationPrefab;

        private void Awake()
        {
            Instance = this;
        }

        public void PlayDeathAnimation(Vector3 position)
        {
            var effect = Instantiate(_deathAnimationPrefab, position, Quaternion.identity);
            Destroy(effect, 1);
        }
    }
}
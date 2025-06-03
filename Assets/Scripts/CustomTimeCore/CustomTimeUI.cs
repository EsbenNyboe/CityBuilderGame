using System.Globalization;
using TMPro;
using UnityEngine;

namespace CustomTimeCore
{
    public class CustomTimeUI : MonoBehaviour
    {
        public static CustomTimeUI Instance;

        public static bool HasUnitSelection;

        [Range(0.01f, 10)] public float TimeScale;

        [SerializeField] private float _maxTimeScale;
        [SerializeField] private float _minTimeScale;

        [SerializeField] private TextMeshProUGUI _timeText;

        private void Awake()
        {
            Instance = this;
            UpdateText();
        }

        private void Update()
        {
            if (HasUnitSelection)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                IncreaseTimeScale();
            }

            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                DecreaseTimeScale();
            }
        }

        public void IncreaseTimeScale()
        {
            TimeScale *= 2;
            if (TimeScale > _maxTimeScale)
            {
                TimeScale = _maxTimeScale;
            }

            UpdateText();
        }

        public void DecreaseTimeScale()
        {
            TimeScale /= 2;
            if (TimeScale < _minTimeScale)
            {
                TimeScale = _minTimeScale;
            }

            UpdateText();
        }

        private void UpdateText()
        {
            _timeText.text = "Time: " + TimeScale.ToString(CultureInfo.InvariantCulture);
        }
    }
}
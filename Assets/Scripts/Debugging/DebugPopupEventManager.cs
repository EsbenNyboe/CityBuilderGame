using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Debugging
{
    public class DebugPopupEventManager : MonoBehaviour
    {
        public static DebugPopupEventManager Instance;
        [SerializeField] private GameObject _popupPrefab;
        [SerializeField] private Canvas _canvas;

        private void Awake()
        {
            Instance = this;
        }

        public void ShowPopup(Vector3 position, float duration, DebugPopupEventType eventType, Color debugColor)
        {
            var popup = Instantiate(_popupPrefab, _canvas.transform);
            popup.GetComponent<Transform>().position = position;
            popup.GetComponentInChildren<TextMeshProUGUI>().text = eventType.ToString();
            popup.GetComponentInChildren<Image>().color = debugColor;
            Destroy(popup, duration);
        }
    }
}
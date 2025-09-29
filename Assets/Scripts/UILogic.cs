using UnityEngine;
using UnityEngine.UI;

public class UILogic : MonoBehaviour
{
    [SerializeField] private Image _uiBackground;

    public void SetBackgroundEnabled(bool enable)
    {
        _uiBackground.enabled = enable;
    }
}
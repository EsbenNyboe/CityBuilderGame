using UnityEngine;
using UnityEngine.UI;

public class TimedImage : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private float _duration;
    private float _timeOfEnable;

    private void Update()
    {
        if (_timeOfEnable + _duration < Time.time)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        _timeOfEnable = Time.time;
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void SetParent(Transform parent)
    {
        _rectTransform.SetParent(parent);
    }
}
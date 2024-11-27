using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class GridDebugUI : MonoBehaviour
{
    [SerializeField] private Font _font;

    [Range(0, 1)] [SerializeField] private float _offsetPercentX;
    [Range(0, 1)] [SerializeField] private float _offsetPercentY;

    private Text[] _debugTextArrayX;
    private Text[] _debugTextArrayY;

    private Transform _uiParent;

    private bool _isInitialized;

    private void Awake()
    {
        _uiParent = GetComponentInChildren<Canvas>().transform;
    }

    private void LateUpdate()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        float3 cameraPosition = camera.transform.position;
        var screenRatio = Screen.width / (float)Screen.height;
        var cameraSizeX = camera.orthographicSize * screenRatio;
        var cameraSizeY = camera.orthographicSize;

        var xLeft = cameraPosition.x - cameraSizeX + cameraSizeY * _offsetPercentX;
        var yTop = cameraPosition.y + cameraSizeY - cameraSizeY * _offsetPercentY;

        if (!World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GridManager>())
                .TryGetSingleton<GridManager>(out var gridManager) ||
            !gridManager.IsInitialized())
        {
            return;
        }

        if (!_isInitialized)
        {
            _isInitialized = true;

            _debugTextArrayX = new Text[gridManager.Width];
            _debugTextArrayY = new Text[gridManager.Height];

            for (var x = 0; x < _debugTextArrayX.Length; x++)
            {
                var uiPosition = camera.WorldToScreenPoint(new Vector2(x, yTop));
                _debugTextArrayX[x] = UtilsClass.DrawTextUI(x.ToString(), _uiParent, uiPosition, 20, _font);
                _debugTextArrayX[x].alignment = TextAnchor.MiddleCenter;
            }

            for (var y = 0; y < _debugTextArrayY.Length; y++)
            {
                var uiPosition = camera.WorldToScreenPoint(new Vector2(xLeft, y));
                _debugTextArrayY[y] = UtilsClass.DrawTextUI(y.ToString(), _uiParent, uiPosition, 20, _font);
                _debugTextArrayX[y].alignment = TextAnchor.MiddleCenter;
            }
        }

        for (var x = 0; x < _debugTextArrayX.Length; x++)
        {
            var uiPosition = camera.WorldToScreenPoint(new Vector2(x, yTop));
            var localUiPosition = _uiParent.InverseTransformPoint(uiPosition);
            _debugTextArrayX[x].rectTransform.anchoredPosition = localUiPosition;
        }

        for (var y = 0; y < _debugTextArrayY.Length; y++)
        {
            var uiPosition = camera.WorldToScreenPoint(new Vector2(xLeft, y));
            var localUiPosition = _uiParent.InverseTransformPoint(uiPosition);
            _debugTextArrayY[y].rectTransform.anchoredPosition = localUiPosition;
        }
    }
}
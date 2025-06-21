using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Grid.SaveLoad
{
    public class SaveSlotUI : MonoBehaviour
    {
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        [SerializeField] private Image _image;
        [SerializeField] private TextMeshProUGUI _sizeText;
        [SerializeField] private TextMeshProUGUI _titleText;

        private Texture2D _texture2D;
        private float _maxImageWidth;
        private float _maxImageHeight;

        private void OnDestroy()
        {
            if (_texture2D)
            {
                Destroy(_texture2D);
            }
        }

        public void Initialize(SavedGridStateObject stateObject)
        {
            _titleText.text = stateObject.name;
            _sizeText.text = stateObject.GridSize.x + "x" + stateObject.GridSize.y;

            if (_texture2D)
            {
                Destroy(_texture2D);
            }

            var gridSizeX = stateObject.GridSize.x > 0 ? stateObject.GridSize.x : GridManagerSystem.DefaultWidth;
            var gridSizeY = stateObject.GridSize.y > 0 ? stateObject.GridSize.y : GridManagerSystem.DefaultHeight;
            _texture2D = new Texture2D(gridSizeX, gridSizeY);

            if (_maxImageWidth <= 0)
            {
                _maxImageWidth = _image.rectTransform.sizeDelta.x;
                _maxImageHeight = _image.rectTransform.sizeDelta.y;
            }

            var gridRatio = (float)gridSizeX / gridSizeY;

            var imageIsFlat = gridRatio > 1;

            var newWidth = imageIsFlat ? _maxImageWidth : _maxImageWidth * gridRatio;
            var newHeight = imageIsFlat ? _maxImageHeight / gridRatio : _maxImageHeight;
            _image.rectTransform.sizeDelta = new Vector2(newWidth, newHeight);

            // Black background
            for (var y = 0; y < _texture2D.height; y++)
            {
                for (var x = 0; x < _texture2D.width; x++)
                {
                    var color = Color.black;
                    _texture2D.SetPixel(x, y, color);
                }
            }

            for (var i = 0; i < stateObject.Trees.Length; i++)
            {
                _texture2D.SetPixel(stateObject.Trees[i].x, stateObject.Trees[i].y, Color.green);
            }

            for (var i = 0; i < stateObject.Beds.Length; i++)
            {
                _texture2D.SetPixel(stateObject.Beds[i].x, stateObject.Beds[i].y, Color.white);
            }

            for (var i = 0; i < stateObject.Storages.Length; i++)
            {
                _texture2D.SetPixel(stateObject.Storages[i].x, stateObject.Storages[i].y, Color.red);
            }

            for (var i = 0; i < stateObject.Villagers.Length; i++)
            {
                _texture2D.SetPixel(Mathf.FloorToInt(stateObject.Villagers[i].x), Mathf.FloorToInt(stateObject.Villagers[i].y), Color.blue);
            }

            for (var i = 0; i < stateObject.Boars.Length; i++)
            {
                _texture2D.SetPixel(Mathf.FloorToInt(stateObject.Boars[i].x), Mathf.FloorToInt(stateObject.Boars[i].y), Color.magenta);
            }

            _texture2D.Apply();
            _image.material.SetTexture(MainTex, _texture2D);
        }

        public void Save()
        {
            SavedGridStateManager.Instance.SaveToSlot(transform);
        }

        public void Load()
        {
            SavedGridStateManager.Instance.LoadFromSlot(transform);
        }

        public void Delete()
        {
            SavedGridStateManager.Instance.DeleteDataInSlot(transform);
        }
    }
}
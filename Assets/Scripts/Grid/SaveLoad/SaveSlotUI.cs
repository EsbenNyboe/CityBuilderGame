using Grid.SaveLoad;
using UnityEngine;

public class SaveSlotUI : MonoBehaviour
{
    [SerializeField] private RectTransform _imageTransform;

    private Rect _textureRect;
    private Texture2D _texture2D;

    private void OnDestroy()
    {
        if (_texture2D)
        {
            Destroy(_texture2D);
        }
    }

    private void OnGUI()
    {
        if (_texture2D)
        {
            _textureRect = _imageTransform.rect;
            _textureRect.position = _imageTransform.TransformPoint(_textureRect.position);
            Graphics.DrawTexture(_textureRect, _texture2D);
        }
    }

    public void Initialize(SavedGridStateObject _stateObject)
    {
        // TODO: Save gridManager size in state-object
        _texture2D = new Texture2D(GridManagerSystem.Width, GridManagerSystem.Height);

        // Black background
        for (var y = 0; y < _texture2D.height; y++)
        {
            for (var x = 0; x < _texture2D.width; x++)
            {
                var color = Color.black;
                _texture2D.SetPixel(x, y, color);
            }
        }

        for (var i = 0; i < _stateObject.Trees.Length; i++)
        {
            _texture2D.SetPixel(_stateObject.Trees[i].x, _stateObject.Trees[i].y, Color.green);
        }

        for (var i = 0; i < _stateObject.Beds.Length; i++)
        {
            _texture2D.SetPixel(_stateObject.Beds[i].x, _stateObject.Beds[i].y, Color.white);
        }

        for (var i = 0; i < _stateObject.DropPoints.Length; i++)
        {
            _texture2D.SetPixel(_stateObject.DropPoints[i].x, _stateObject.DropPoints[i].y, Color.red);
        }

        _texture2D.Apply();
    }

    public void Save()
    {
        GetComponentInParent<SavedGridStateManager>().SaveToSlot(transform);
    }

    public void Load()
    {
        GetComponentInParent<SavedGridStateManager>().LoadFromSlot(transform);
    }

    public void Delete()
    {
        GetComponentInParent<SavedGridStateManager>().DeleteDataInSlot(transform);
    }
}
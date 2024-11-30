using Grid.SaveLoad;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class SaveSlotUI : MonoBehaviour
{
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    [SerializeField] private Image _image;

    private Texture2D _texture2D;

    private void Start()
    {
        Assert.IsNotNull(_image);
        _image.material.SetTexture(MainTex, _texture2D);
    }

    private void OnDestroy()
    {
        if (_texture2D)
        {
            Destroy(_texture2D);
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
        _image.material.SetTexture(MainTex, _texture2D);
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
using Grid.SaveLoad;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotUI : MonoBehaviour
{
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    [SerializeField] private Image _image;

    private Texture2D _texture2D;

    private void OnDestroy()
    {
        if (_texture2D)
        {
            Destroy(_texture2D);
        }
    }

    public void Initialize(SavedGridStateObject stateObject)
    {
        if (_texture2D)
        {
            Destroy(_texture2D);
        }

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

        for (var i = 0; i < stateObject.Trees.Length; i++)
        {
            _texture2D.SetPixel(stateObject.Trees[i].x, stateObject.Trees[i].y, Color.green);
        }

        for (var i = 0; i < stateObject.Beds.Length; i++)
        {
            _texture2D.SetPixel(stateObject.Beds[i].x, stateObject.Beds[i].y, Color.white);
        }

        for (var i = 0; i < stateObject.DropPoints.Length; i++)
        {
            _texture2D.SetPixel(stateObject.DropPoints[i].x, stateObject.DropPoints[i].y, Color.red);
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
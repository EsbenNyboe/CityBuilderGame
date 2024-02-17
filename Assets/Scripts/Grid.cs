using System;
using System.Collections;
using System.Collections.Generic;
using CodeMonkey.Utils;
using UnityEngine;

public class Grid<TGridObject>
{
    private int _width;
    private int _height;
    private float _cellSize;
    private Vector3 _originPosition;
    private TGridObject[,] _gridArray;
    private TextMesh[,] _debugTextArray;

    public event EventHandler<OnGridValueChangedEventArgs> OnGridValueChanged;
    public class OnGridValueChangedEventArgs : EventArgs
    {
        public int x;
        public int y;
    }

    public Grid(int width, int height, float cellSize, Vector3 originPosition)
    {
        _width = width;
        _height = height;
        _cellSize = cellSize;
        _originPosition = originPosition;

        _gridArray = new TGridObject[width, height];
        _debugTextArray = new TextMesh[width, height];

        for (int x = 0; x < _gridArray.GetLength(0); x++)
        {
            for (int y = 0; y < _gridArray.GetLength(1); y++)
            {
                _debugTextArray[x, y] = UtilsClass.CreateWorldText(
                    _gridArray[x, y].ToString(),
                    null,
                    GetPositionAtCoordinate(x, y) + new Vector3(cellSize, cellSize) * 0.5f,
                    20,
                    Color.white,
                    TextAnchor.MiddleCenter);
                Debug.DrawLine(GetPositionAtCoordinate(x, y), GetPositionAtCoordinate(x, y + 1), Color.white, 100f);
                Debug.DrawLine(GetPositionAtCoordinate(x, y), GetPositionAtCoordinate(x + 1, y), Color.white, 100f);
            }

            Debug.DrawLine(GetPositionAtCoordinate(0, height), GetPositionAtCoordinate(width, height), Color.white, 100f);
            Debug.DrawLine(GetPositionAtCoordinate(width, 0), GetPositionAtCoordinate(width, height), Color.white, 100f);
        }
    }

    public int GetWidth()
    {
        return _width;
    }

    public int GetHeight()
    {
        return _height;
    }
    public float GetCellSize()
    {
        return _cellSize;
    }

    public void SetValueAtPosition(Vector3 worldPosition, TGridObject value)
    {
        int x, y;
        GetCoordinateAtPosition(worldPosition, out x, out y);
        SetValueAtCoordinate(x, y, value);
    }

    public void SetValueAtCoordinate(int x, int y, TGridObject value)
    {
        if (x < 0 || y < 0 || x >= _width || y >= _height)
        {
            return;
        }

        _gridArray[x, y] = value;
        _debugTextArray[x, y].text = _gridArray[x, y].ToString();
        if (OnGridValueChanged != null)
        {
            OnGridValueChanged(this, new OnGridValueChangedEventArgs { x = x, y = y });
        }
    }

    public TGridObject GetValueAtPosition(Vector3 worldPosition)
    {
        int x, y;
        GetCoordinateAtPosition(worldPosition, out x, out y);
        return GetValueAtCoordinate(x, y);
    }

    public TGridObject GetValueAtCoordinate(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _width || y >= _height)
        {
            return default(TGridObject);
        }

        return _gridArray[x, y];
    }

    public Vector3 GetPositionAtCoordinate(int x, int y)
    {
        return new Vector3(x, y) * _cellSize + _originPosition;
    }

    private void GetCoordinateAtPosition(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition.x - _originPosition.x) / _cellSize);
        y = Mathf.FloorToInt((worldPosition.y - _originPosition.y) / _cellSize);
    }    
}
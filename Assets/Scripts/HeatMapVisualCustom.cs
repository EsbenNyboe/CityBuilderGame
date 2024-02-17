using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatMapVisualCustom : MonoBehaviour
{
    private const float HeatMapMaxValue = 100f;

    private Grid<int> _grid;
    private Mesh _mesh;

    private void Awake()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
    }

    public void SetGrid(Grid<int> grid)
    {
        _grid = grid;
        UpdateHeatVisuals();

        _grid.OnGridValueChanged += Grid_OnGridValueChanged;
    }

    private void Grid_OnGridValueChanged(object sender, Grid<int>.OnGridValueChangedEventArgs e)
    {
        UpdateHeatVisuals();
    }

    private void UpdateHeatVisuals()
    {
        MeshUtils.CreateEmptyMeshArrays(_grid.GetWidth() * _grid.GetHeight(), out Vector3[] vertices, out Vector2[] uv, out int[] triangles);

        for (int x = 0; x < _grid.GetWidth(); x++)
        {
            for (int y = 0; y < _grid.GetHeight(); y++)
            {
                int index = x * _grid.GetHeight() + y;
                Vector3 quadSize = new Vector3(1, 1) * _grid.GetCellSize();

                int gridValue = _grid.GetValueAtCoordinate(x, y);
                float gridValueNormalized = gridValue / HeatMapMaxValue;
                Vector2 gridValueUV = new Vector2(gridValueNormalized, 0f);

                MeshUtils.AddToMeshArrays(vertices, uv, triangles, index, _grid.GetPositionAtCoordinate(x, y) + quadSize * 0.5f, 0f, quadSize, gridValueUV, gridValueUV);
            }
        }

        _mesh.vertices = vertices;
        _mesh.uv = uv;
        _mesh.triangles = triangles;
    }

    private void AddValueAtCoordinate(int x, int y, int value)
    {
        var newValue = _grid.GetValueAtCoordinate(x, y) + value;
        _grid.SetValueAtCoordinate(x, y, newValue);
    }

    public void AddValueAtPosition(Vector3 position, int value, int range)
    {
        _grid.GetCoordinateAtPosition(position, out int originX, out int originY);
        for (int x = 0; x < range; x++)
        {
            for (int y = 0; y < range - x; y++)
            {
                AddValueAtCoordinate(originX + x, originY + y, value);

                if (x != 0)
                {
                    AddValueAtCoordinate(originX - x, originY + y, value);
                }

                if (y != 0)
                {
                    AddValueAtCoordinate(originX + x, originY - y, value);

                    if (x != 0)
                    {
                        AddValueAtCoordinate(originX - x, originY - y, value);
                    }
                }
            }
        }
    }
}
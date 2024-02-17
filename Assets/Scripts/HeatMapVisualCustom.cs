using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatMapVisualCustom : MonoBehaviour
{
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
        Debug.Log("Grid updated");
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

                MeshUtils.AddToMeshArrays(vertices, uv, triangles, index, _grid.GetPositionAtCoordinate(x, y) + quadSize * 0.5f, 0f, quadSize, Vector2.zero, Vector2.zero);
            }
        }

        _mesh.vertices = vertices;
        _mesh.uv = uv;
        _mesh.triangles = triangles;
    }

}
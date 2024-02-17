using System;
using System.Collections.Generic;
using CodeMonkey.Utils;
using UnityEngine;

public class GridTestingScript : MonoBehaviour
{

    [SerializeField] private HeatMapVisualCustom _heatMapVisual;
    private Grid<int> _grid;

    [SerializeField] private float _cellDiameter;

    private void Start()
    {
        _grid = new Grid<int>(4, 4, _cellDiameter, new Vector3(-20, -10));

        _heatMapVisual.SetGrid(_grid);
    }

    private void Update()
    {
        //if (Input.GetMouseButton(0))
        //{
        //    _grid.SetValueAtPosition(UtilsClass.GetMouseWorldPosition(), 1);
        //}



        if (Input.GetMouseButtonDown(0))
        {
            var mousePosition = UtilsClass.GetMouseWorldPosition();
            var currentValue = _grid.GetValueAtPosition(mousePosition);
            _grid.SetValueAtPosition(mousePosition, currentValue + 5);
        }

        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log(_grid.GetValueAtPosition(UtilsClass.GetMouseWorldPosition()));
        }
    }
}
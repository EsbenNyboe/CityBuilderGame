using System;
using System.Collections.Generic;
using CodeMonkey.Utils;
using UnityEngine;

public class GridTestingScript : MonoBehaviour
{
    private Grid<bool> _grid;

    private void Start()
    {
        _grid = new Grid<bool>(4, 2, 10f, new Vector3(-20, -10));

        var boolList = new List<bool>();
        var intList = new List<int>();
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            _grid.SetValueAtPosition(UtilsClass.GetMouseWorldPosition(), true);
        }

        //if (Input.GetMouseButtonDown(0))
        //{
        //    _grid.SetValueAtPosition(UtilsClass.GetMouseWorldPosition(), 56);
        //}

        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log(_grid.GetValueAtPosition(UtilsClass.GetMouseWorldPosition()));
        }
    }
}
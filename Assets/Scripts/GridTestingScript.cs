using System;
using CodeMonkey.Utils;
using UnityEngine;

public class GridTestingScript : MonoBehaviour
{
    private Grid _grid;

    private void Start()
    {
        _grid = new Grid(4, 2, 10f, new Vector3(-20, -10));
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _grid.SetValueAtPosition(UtilsClass.GetMouseWorldPosition(), 56);
        }

        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log(_grid.GetValueAtPosition(UtilsClass.GetMouseWorldPosition()));
        }
    }
}
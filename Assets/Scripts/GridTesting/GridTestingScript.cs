// using System;
// using System.Collections.Generic;
// using CodeMonkey.Utils;
// using UnityEngine;
//
// public class GridTestingScript : MonoBehaviour
// {
//
//     [SerializeField] private HeatMapVisualCustom _heatMapVisual;
//     private Grid<int> _grid;
//
//     [SerializeField] private int _width;
//     [SerializeField] private int _height;
//     [SerializeField] private float _cellDiameter;
//
//     private void Start()
//     {
//         _grid = new Grid<int>(30, 15, 1f, Vector3.zero, (Grid<int> grid, int x, int y) => );
//
//         _heatMapVisual.SetGrid(_grid);
//     }
//
//     private void Update()
//     {
//         if (Input.GetMouseButtonDown(0))
//         {
//             var mousePosition = UtilsClass.GetMouseWorldPosition();
//             var currentValue = _grid.GetGridObject(mousePosition);
//             //_grid.SetValueAtPosition(mousePosition, currentValue + 5);
//
//             _heatMapVisual.AddValueAtPosition(mousePosition, 100, 5, 40);
//         }
//
//         if (Input.GetMouseButtonDown(1))
//         {
//             Debug.Log(_grid.GetGridObject(UtilsClass.GetMouseWorldPosition()));
//         }
//     }
// }


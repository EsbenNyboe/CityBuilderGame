/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using CodeMonkey;
using Unity.Entities;

public class PathfindingGridSetup : MonoBehaviour {

    public static PathfindingGridSetup Instance { private set; get; }

    [SerializeField] private PathfindingVisual pathfindingVisual;
    public Grid<GridNode> pathfindingGrid;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        pathfindingGrid = new Grid<GridNode>(30, 15, 1f, Vector3.zero, (Grid<GridNode> grid, int x, int y) => new GridNode(grid, x, y));

        pathfindingGrid.GetGridObject(2, 0).SetIsWalkable(false);

        pathfindingVisual.SetGrid(pathfindingGrid);
    }

    private void Update() {
        if (Input.GetMouseButtonDown(1)) {
            Vector3 mousePosition = UtilsClass.GetMouseWorldPosition() + (new Vector3(+1, +1) * pathfindingGrid.GetCellSize() * .5f);
            GridNode gridNode = pathfindingGrid.GetGridObject(mousePosition);
            if (gridNode != null) {
                gridNode.SetIsWalkable(!gridNode.IsWalkable());
            }
        }
    }

}

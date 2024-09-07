/*
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using CodeMonkey.Utils;
using UnityEngine;

public class PathfindingGridSetup : MonoBehaviour
{
    [SerializeField] private PathfindingVisual pathfindingVisual;
    public Grid<GridNode> pathfindingGrid;

    private bool _shouldSetToWalkableOnMouseDown;

    public static PathfindingGridSetup Instance { private set; get; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        pathfindingGrid = new Grid<GridNode>(30, 15, 1f, Vector3.zero, ( grid,  x,  y) => new GridNode(grid, x, y));

        pathfindingGrid.GetGridObject(2, 0).SetIsWalkable(false);

        pathfindingVisual.SetGrid(pathfindingGrid);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftControl))
        {
            var mousePosition = UtilsClass.GetMouseWorldPosition() + new Vector3(+1, +1) * pathfindingGrid.GetCellSize() * .5f;
            var gridNode = pathfindingGrid.GetGridObject(mousePosition);
            if (gridNode != null)
            {
                _shouldSetToWalkableOnMouseDown = !gridNode.IsWalkable();

                gridNode.SetIsWalkable(!gridNode.IsWalkable());
            }
        }

        if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftControl))
        {
            var mousePosition = UtilsClass.GetMouseWorldPosition() + new Vector3(+1, +1) * pathfindingGrid.GetCellSize() * .5f;
            var gridNode = pathfindingGrid.GetGridObject(mousePosition);
            if (gridNode != null)
            {
                gridNode.SetIsWalkable(_shouldSetToWalkableOnMouseDown);
            }
        }
    }
}
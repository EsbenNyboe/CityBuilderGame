using Unity.Mathematics;
using UnityEngine;

public class GridHelpers
{
    public static int CalculatePositionListLength(int ringCount)
    {
        var length = 1;
        for (var ringSize = 1; ringSize < ringCount; ringSize++)
        {
            // start at max X & max Y
            var x = ringSize;
            var y = ringSize;

            // go to min X
            while (x > -ringSize)
            {
                length++;
                x--;
            }

            // go to min Y
            while (y > -ringSize)
            {
                length++;
                y--;
            }

            // go to max X
            while (x < ringSize)
            {
                length++;
                x++;
            }

            // go to max Y
            while (y < ringSize)
            {
                length++;
                y++;
            }
        }

        return length;
    }

    #region GenericGrid

    public static int GetIndex(GridManager gridManager, int x, int y)
    {
        return GetIndex(gridManager.Height, x, y);
    }

    public static int GetIndex(int gridHeight, int x, int y)
    {
        return x * gridHeight + y;
    }

    public static void GetXY(Vector3 worldPosition, out int x, out int y)
    {
        // gridManager currently only supports default origin-position (Vector3.zero) and default cellSize (1f)
        x = Mathf.FloorToInt(worldPosition.x);
        y = Mathf.FloorToInt(worldPosition.y);
    }

    public static Vector3 GetWorldPosition(int x, int y)
    {
        // gridManager currently only supports default origin-position (Vector3.zero) and default cellSize (1f)
        return new Vector3(x, y, 0);
    }

    #endregion

    #region GridSearch_Neighbours

    public static bool CellsAreTouching(Vector3 worldPosition, int2 targetCell)
    {
        GetXY(worldPosition, out var centerX, out var centerY);
        return CellsAreTouching(centerX, centerY, targetCell.x, targetCell.y);
    }

    public static bool CellsAreTouching(int2 centerCell, int2 targetCell)
    {
        return CellsAreTouching(centerCell.x, centerCell.y, targetCell.x, targetCell.y);
    }

    public static bool CellsAreTouching(int centerX, int centerY, int targetX, int targetY)
    {
        var xDistance = Mathf.Abs(centerX - targetX);
        var yDistance = Mathf.Abs(centerY - targetY);
        var cellsAreSameOrNeighbours = xDistance is > -1 and < 2 && yDistance is > -1 and < 2;
        return cellsAreSameOrNeighbours;
    }

    #endregion
}
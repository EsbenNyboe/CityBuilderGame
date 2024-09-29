using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Assertions;

public class PathingHelpers
{
    private static readonly List<int2> PositionList = new ();
    private static readonly List<int> NeighbourDeltasX = new()  { 1, 1, 0, -1, -1, -1, 0, 1 };
    private static readonly List<int> NeighbourDeltasY = new()  { 0, 1, 1, 1, 0, -1, -1, -1 };

    public static void ValidateGridPosition(ref int x, ref int y)
    {
        x = math.clamp(x, 0, GridSetup.Instance.PathGrid.GetWidth() - 1);
        y = math.clamp(y, 0, GridSetup.Instance.PathGrid.GetHeight() - 1);
    }

    public static bool IsPositionInsideGrid(int2 gridPosition)
    {
        return
            gridPosition.x >= 0 &&
            gridPosition.y >= 0 &&
            gridPosition.x < GridSetup.Instance.PathGrid.GetWidth() &&
            gridPosition.y < GridSetup.Instance.PathGrid.GetHeight();
    }

    public static bool IsPositionInsideGrid(int x, int y)
    {
        return
            x >= 0 &&
            y >= 0 &&
            x < GridSetup.Instance.PathGrid.GetWidth() &&
            y < GridSetup.Instance.PathGrid.GetHeight();
    }

    public static bool IsPositionWalkable(int2 gridPosition)
    {
        return GridSetup.Instance.PathGrid.GetGridObject(gridPosition.x, gridPosition.y).IsWalkable();
    }

    public static bool IsPositionOccupied(int2 gridPosition)
    {
        return GridSetup.Instance.OccupationGrid.GetGridObject(gridPosition.x, gridPosition.y).IsOccupied();
    }

    public static List<int2> GetCellListAroundTargetCell(int2 firstPosition, int ringCount)
    {
        PositionList.Clear();
        PositionList.Add(firstPosition);

        for (var i = 1; i < ringCount; i++)
        {
            for (var j = 1; j < i; j++)
            {
                AddFourPositionsAroundTarget(PositionList, firstPosition, i, j);
                AddFourPositionsAroundTarget(PositionList, firstPosition, j, i);
            }

            if (i - 1 > 0)
            {
                AddFourPositionsAroundTarget(PositionList, firstPosition, i - 1, i - 1);
            }

            PositionList.Add(firstPosition + new int2(i, 0));
            PositionList.Add(firstPosition + new int2(-i, 0));
            PositionList.Add(firstPosition + new int2(0, i));
            PositionList.Add(firstPosition + new int2(0, -i));
        }

        return PositionList;
    }

    private static void AddFourPositionsAroundTarget(List<int2> positionList, int2 firstPosition, int a, int b)
    {
        // if (positionList.Contains(firstPosition + new int2(a, b)))
        // {
        //     for (var i = 0; i < positionList.Count; i++)
        //     {
        //         if (positionList[i].Equals(firstPosition + new int2(a, b)))
        //         {
        //             Debug.Log("DUPLICATE IS THIS: " + i);
        //         }
        //     }
        //
        //     Debug.Log("Duplicate found on index: " + positionList.Count);
        // }
        //
        // if (positionList.Contains(firstPosition + new int2(-a, -b)))
        // {
        //     Debug.Log("Duplicate found on index: " + positionList.Count);
        // }
        //
        // if (positionList.Contains(firstPosition + new int2(-a, b)))
        // {
        //     Debug.Log("Duplicate found on index: " + positionList.Count);
        // }
        //
        // if (positionList.Contains(firstPosition + new int2(a, -b)))
        // {
        //     Debug.Log("Duplicate found on index: " + positionList.Count);
        // }

        positionList.Add(firstPosition + new int2(a, b));
        positionList.Add(firstPosition + new int2(-a, -b));
        positionList.Add(firstPosition + new int2(-a, b));
        positionList.Add(firstPosition + new int2(a, -b));
    }

    public static void GetNeighbourCell(int index, int x, int y, out int neighbourX, out int neighbourY)
    {
        Assert.IsTrue(index >= 0 && index <= 8, "Index must be min 0 and max 8, because a cell can only have 8 neighbours!");

        neighbourX = x + NeighbourDeltasX[index];
        neighbourY = y + NeighbourDeltasY[index];
    }
}
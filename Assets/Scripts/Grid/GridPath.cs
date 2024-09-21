public class GridPath
{
    private readonly Grid<GridPath> _grid;
    private readonly int _x;
    private readonly int _y;

    private bool _isWalkable;

    public GridPath(Grid<GridPath> grid, int x, int y)
    {
        _grid = grid;
        _x = x;
        _y = y;
        _isWalkable = true;
    }

    public bool IsWalkable()
    {
        return _isWalkable;
    }

    public void SetIsWalkable(bool isWalkable)
    {
        _isWalkable = isWalkable;
        _grid.TriggerGridObjectChanged(_x, _y);
    }
}
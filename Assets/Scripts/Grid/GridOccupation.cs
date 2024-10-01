using Unity.Entities;

public class GridOccupation
{
    private const bool ShowDebug = false;

    private readonly Grid<GridOccupation> _grid;
    private readonly int _x;
    private readonly int _y;

    private Entity _owner;

    public GridOccupation(Grid<GridOccupation> grid, int x, int y)
    {
        _grid = grid;
        _x = x;
        _y = y;
        _owner = Entity.Null;
    }

    public bool IsOccupied()
    {
        return _owner != Entity.Null && World.DefaultGameObjectInjectionWorld.EntityManager.Exists(_owner);
    }

    public Entity GetOwner()
    {
        return _owner;
    }

    public bool EntityIsOwner(Entity entityToCheck)
    {
        return entityToCheck == _owner;
    }

    public void SetOccupied(Entity newOwner)
    {
        _owner = newOwner;

        if (ShowDebug)
        {
            _grid.TriggerGridObjectChanged(_x, _y);
        }
    }
}
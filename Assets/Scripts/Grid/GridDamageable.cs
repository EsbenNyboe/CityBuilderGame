public class GridDamageable
{
    private readonly Grid<GridDamageable> _grid;
    private readonly int _x;
    private readonly int _y;

    private float _health;

    public GridDamageable(Grid<GridDamageable> grid, int x, int y)
    {
        _grid = grid;
        _x = x;
        _y = y;
        _health = 0;
    }

    public bool IsDamageable()
    {
        return _health > 0;
    }

    public float GetHealth()
    {
        return _health;
    }

    public void SetHealth(float health)
    {
        _health = health;
        _grid.TriggerGridObjectChanged(_x, _y);
    }
}
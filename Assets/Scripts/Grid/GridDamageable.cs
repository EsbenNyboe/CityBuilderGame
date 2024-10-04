using UnityEngine;

public class GridDamageable
{
    private readonly Grid<GridDamageable> _grid;
    private readonly int _x;
    private readonly int _y;

    private float _health;
    private float _maxHealth;

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

    public float GetMaxHealth()
    {
        return _maxHealth;
    }

    public void SetHealth(float health)
    {
        _health = health;
        _maxHealth = _health;
        _grid.TriggerGridObjectChanged(_x, _y);
    }

    public void RemoveFromHealth(float delta)
    {
        AddToHealth(-delta);
    }

    public void AddToHealth(float delta)
    {
        _health += delta;
        _grid.TriggerGridObjectChanged(_x, _y);
    }
}
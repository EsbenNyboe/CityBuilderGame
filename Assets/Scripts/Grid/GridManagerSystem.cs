using Unity.Collections;
using Unity.Entities;

public struct GridManager : IComponentData
{
    public int Width;
    public int Height;
    public NativeArray<WalkableCell> WalkableGrid;
    public NativeArray<DamageableCell> DamageableGrid;
    public NativeArray<OccupiableCell> OccupiableGrid;
    public bool WalkableGridIsDirty;
    public bool DamageableGridIsDirty;
    public bool OccupiableGridIsDirty;
}

public struct WalkableCell
{
    public bool IsWalkable;
    public bool IsDirty;
}

public struct DamageableCell
{
    public float Health;
    public float MaxHealth;
    public bool IsDirty;
}

public struct OccupiableCell
{
    public Entity Occupant;
    public bool IsDirty;
}

public partial class GridManagerSystem : SystemBase
{
    private const int MaxHealth = 100;

    protected override void OnCreate()
    {
        var width = 105;
        var height = 95;

        var walkableGrid = new NativeArray<WalkableCell>(width * height, Allocator.Persistent);
        for (var i = 0; i < walkableGrid.Length; i++)
        {
            var cell = walkableGrid[i];
            cell.IsWalkable = true;
            cell.IsDirty = true;
            walkableGrid[i] = cell;
        }

        var damageableGrid = new NativeArray<DamageableCell>(width * height, Allocator.Persistent);
        for (var i = 0; i < damageableGrid.Length; i++)
        {
            var cell = damageableGrid[i];
            cell.Health = 0;
            cell.MaxHealth = MaxHealth;
            cell.IsDirty = true;
            damageableGrid[i] = cell;
        }

        var occupiableGrid = new NativeArray<OccupiableCell>(width * height, Allocator.Persistent);
        for (var i = 0; i < occupiableGrid.Length; i++)
        {
            var cell = occupiableGrid[i];
            cell.Occupant = Entity.Null;
            cell.IsDirty = true;
            occupiableGrid[i] = cell;
        }

        EntityManager.AddComponent<GridManager>(SystemHandle);
        SystemAPI.SetComponent(SystemHandle, new GridManager
        {
            Width = width,
            Height = height,
            WalkableGrid = walkableGrid,
            DamageableGrid = damageableGrid,
            OccupiableGrid = occupiableGrid,
            WalkableGridIsDirty = true,
            DamageableGridIsDirty = true,
            OccupiableGridIsDirty = true
        });
    }

    protected override void OnUpdate()
    {
    }

    protected override void OnDestroy()
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(SystemHandle);
        for (var i = 0; i < gridManager.WalkableGrid.Length; i++)
        {
            // Debug.Log("IsWalkable: " + gridManager.WalkableGrid[i].IsWalkable);
        }

        gridManager.WalkableGrid.Dispose();
        gridManager.DamageableGrid.Dispose();
        gridManager.OccupiableGrid.Dispose();
    }
}
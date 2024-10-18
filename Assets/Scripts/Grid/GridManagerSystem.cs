using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public partial struct GridManager : IComponentData
{
    public int Width;
    public int Height;
    public NativeArray<WalkableCell> WalkableGrid;
    public NativeArray<DamageableCell> DamageableGrid;
    public NativeArray<OccupiableCell> OccupiableGrid;
    public bool WalkableGridIsDirty;
    public bool DamageableGridIsDirty;
    public bool OccupiableGridIsDirty;

    // GridSearchHelpers:
    public NativeArray<int2> NeighbourDeltas;
    public NativeList<int> RandomNeighbourIndexList;
    public NativeList<int> RandomNearbyCellIndexList;
    public NativeArray<int2> PositionListWith30Rings;
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

        var neighbourDeltas =
            new NativeArray<int2>(new int2[] { new(1, 0), new(1, 1), new(0, 1), new(-1, 1), new(-1, 0), new(-1, -1), new(0, -1), new(1, -1) },
                Allocator.Persistent);
        var randomNeighbourIndexList = new NativeList<int>(Allocator.Persistent);
        var randomNearbyCellIndexList = new NativeList<int>(Allocator.Persistent);
        var positionListWith30Rings = new NativeArray<int2>(GridHelpers.CalculatePositionListLength(30), Allocator.Persistent);

        SystemAPI.SetComponent(SystemHandle, new GridManager
        {
            Width = width,
            Height = height,
            WalkableGrid = walkableGrid,
            DamageableGrid = damageableGrid,
            OccupiableGrid = occupiableGrid,
            WalkableGridIsDirty = true,
            DamageableGridIsDirty = true,
            OccupiableGridIsDirty = true,
            NeighbourDeltas = neighbourDeltas,
            RandomNeighbourIndexList = randomNeighbourIndexList,
            RandomNearbyCellIndexList = randomNearbyCellIndexList,
            PositionListWith30Rings = positionListWith30Rings
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

        // GridSearchHelpers:
        gridManager.NeighbourDeltas.Dispose();
        gridManager.RandomNeighbourIndexList.Dispose();
        gridManager.RandomNearbyCellIndexList.Dispose();
        gridManager.PositionListWith30Rings.Dispose();
    }
}
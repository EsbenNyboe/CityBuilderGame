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
    public NativeArray<InteractableCell> InteractableGrid;
    public bool WalkableGridIsDirty;
    public bool DamageableGridIsDirty;
    public bool OccupiableGridIsDirty;
    public bool InteractableGridIsDirty;

    // GridSearchHelpers:
    public NativeArray<int2> NeighbourDeltas;
    public int PositionListRadius;
    public NativeArray<int2> PositionList;

    public NativeArray<int2> RelativePositionList;
    public NativeArray<int2> RelativePositionRingInfoList;

    public Random Random;
    public uint RandomSeed;
}

[UpdateInGroup(typeof(GridSystemGroup), OrderFirst = true)]
public partial class GridManagerSystem : SystemBase
{
    private const int MaxHealth = 100;
    public const int Width = 105;
    public const int Height = 95;

    protected override void OnCreate()
    {
        var gridManager = new GridManager();
        gridManager.Width = Width;
        gridManager.Height = Height;

        CreateGrids(ref gridManager);
        CreateGridSearchHelpers(ref gridManager);

        EntityManager.AddComponent<GridManager>(SystemHandle);
        SystemAPI.SetComponent(SystemHandle, gridManager);
    }

    protected override void OnUpdate()
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(SystemHandle);
        gridManager.RandomSeed++;
        gridManager.Random = Random.CreateFromIndex(gridManager.RandomSeed);
        SystemAPI.SetComponent(SystemHandle, gridManager);
    }

    protected override void OnDestroy()
    {
        var gridManager = SystemAPI.GetComponent<GridManager>(SystemHandle);

        gridManager.WalkableGrid.Dispose();
        gridManager.DamageableGrid.Dispose();
        gridManager.OccupiableGrid.Dispose();
        gridManager.InteractableGrid.Dispose();

        // GridSearchHelpers:
        gridManager.NeighbourDeltas.Dispose();
        gridManager.PositionList.Dispose();
        gridManager.RelativePositionList.Dispose();
        gridManager.RelativePositionRingInfoList.Dispose();
    }

    private static void CreateGrids(ref GridManager gridManager)
    {
        gridManager.WalkableGridIsDirty = true;
        gridManager.WalkableGrid = new NativeArray<WalkableCell>(Width * Height, Allocator.Persistent);
        for (var i = 0; i < gridManager.WalkableGrid.Length; i++)
        {
            var cell = gridManager.WalkableGrid[i];
            cell.IsWalkable = true;
            cell.IsDirty = true;
            cell.Section = -1;
            gridManager.WalkableGrid[i] = cell;
        }

        gridManager.DamageableGridIsDirty = true;
        gridManager.DamageableGrid = new NativeArray<DamageableCell>(Width * Height, Allocator.Persistent);
        for (var i = 0; i < gridManager.DamageableGrid.Length; i++)
        {
            var cell = gridManager.DamageableGrid[i];
            cell.Health = 0;
            cell.MaxHealth = MaxHealth;
            cell.IsDirty = true;
            gridManager.DamageableGrid[i] = cell;
        }

        gridManager.OccupiableGridIsDirty = true;
        gridManager.OccupiableGrid = new NativeArray<OccupiableCell>(Width * Height, Allocator.Persistent);
        for (var i = 0; i < gridManager.OccupiableGrid.Length; i++)
        {
            var cell = gridManager.OccupiableGrid[i];
            cell.Occupant = Entity.Null;
            cell.IsDirty = true;
            gridManager.OccupiableGrid[i] = cell;
        }

        gridManager.InteractableGridIsDirty = true;
        gridManager.InteractableGrid = new NativeArray<InteractableCell>(Width * Height, Allocator.Persistent);
        for (var i = 0; i < gridManager.InteractableGrid.Length; i++)
        {
            var cell = gridManager.InteractableGrid[i];
            cell.InteractableCellType = InteractableCellType.None;
            cell.IsDirty = true;
            gridManager.InteractableGrid[i] = cell;
        }
    }

    private static void CreateGridSearchHelpers(ref GridManager gridManager)
    {
        gridManager.NeighbourDeltas =
            new NativeArray<int2>(
                new int2[]
                {
                    new(1, 0), new(1, 1), new(0, 1), new(-1, 1), new(-1, 0), new(-1, -1), new(0, -1), new(1, -1)
                },
                Allocator.Persistent);
        gridManager.PositionListRadius = 50;
        gridManager.PositionList =
            new NativeArray<int2>(GridHelpers.CalculatePositionListLength(gridManager.PositionListRadius),
                Allocator.Persistent);

        var relativePositionListRadius = 50;
        gridManager.RelativePositionList =
            new NativeArray<int2>(GridHelpers.CalculatePositionListLength(relativePositionListRadius),
                Allocator.Persistent);
        gridManager.RelativePositionRingInfoList =
            new NativeArray<int2>(relativePositionListRadius, Allocator.Persistent);
        gridManager.PopulateRelativePositionList(relativePositionListRadius);
    }
}
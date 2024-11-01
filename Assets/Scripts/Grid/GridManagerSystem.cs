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

    // GridSearchHelpers:
    public NativeArray<int2> NeighbourDeltas;
    public int NeighbourSequenceIndex;
    public NativeList<int> RandomNearbyCellIndexList;
    public int PositionListRadius;
    public NativeArray<int2> PositionList;

    public NativeArray<int2> RelativePositionList;
    public NativeArray<int2> RelativePositionRingInfoList;
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

public struct InteractableCell
{
    public Entity Interactable;
    public Entity Interactor;
}

public partial class GridManagerSystem : SystemBase
{
    private const int MaxHealth = 100;
    private const int width = 105;
    private const int height = 95;

    protected override void OnCreate()
    {
        var gridManager = new GridManager();
        gridManager.Width = width;
        gridManager.Height = height;

        CreateGrids(ref gridManager);
        CreateGridSearchHelpers(ref gridManager);

        EntityManager.AddComponent<GridManager>(SystemHandle);
        SystemAPI.SetComponent(SystemHandle, gridManager);
    }

    protected override void OnUpdate()
    {
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
        gridManager.RandomNearbyCellIndexList.Dispose();
        gridManager.PositionList.Dispose();
        gridManager.RelativePositionList.Dispose();
        gridManager.RelativePositionRingInfoList.Dispose();
    }

    private static void CreateGrids(ref GridManager gridManager)
    {
        gridManager.WalkableGridIsDirty = true;
        gridManager.WalkableGrid = new NativeArray<WalkableCell>(width * height, Allocator.Persistent);
        for (var i = 0; i < gridManager.WalkableGrid.Length; i++)
        {
            var cell = gridManager.WalkableGrid[i];
            cell.IsWalkable = true;
            cell.IsDirty = true;
            gridManager.WalkableGrid[i] = cell;
        }

        gridManager.DamageableGridIsDirty = true;
        gridManager.DamageableGrid = new NativeArray<DamageableCell>(width * height, Allocator.Persistent);
        for (var i = 0; i < gridManager.DamageableGrid.Length; i++)
        {
            var cell = gridManager.DamageableGrid[i];
            cell.Health = 0;
            cell.MaxHealth = MaxHealth;
            cell.IsDirty = true;
            gridManager.DamageableGrid[i] = cell;
        }

        gridManager.OccupiableGridIsDirty = true;
        gridManager.OccupiableGrid = new NativeArray<OccupiableCell>(width * height, Allocator.Persistent);
        for (var i = 0; i < gridManager.OccupiableGrid.Length; i++)
        {
            var cell = gridManager.OccupiableGrid[i];
            cell.Occupant = Entity.Null;
            cell.IsDirty = true;
            gridManager.OccupiableGrid[i] = cell;
        }

        gridManager.InteractableGrid = new NativeArray<InteractableCell>(width * height, Allocator.Persistent);
        for (var i = 0; i < gridManager.InteractableGrid.Length; i++)
        {
            var cell = gridManager.InteractableGrid[i];
            cell.Interactable = Entity.Null;
            cell.Interactor = Entity.Null;
            gridManager.InteractableGrid[i] = cell;
        }
    }

    private static void CreateGridSearchHelpers(ref GridManager gridManager)
    {
        gridManager.NeighbourDeltas =
            new NativeArray<int2>(new int2[] { new(1, 0), new(1, 1), new(0, 1), new(-1, 1), new(-1, 0), new(-1, -1), new(0, -1), new(1, -1) },
                Allocator.Persistent);
        gridManager.RandomNearbyCellIndexList = new NativeList<int>(Allocator.Persistent);
        gridManager.PositionListRadius = 50;
        gridManager.PositionList =
            new NativeArray<int2>(GridHelpers.CalculatePositionListLength(gridManager.PositionListRadius), Allocator.Persistent);

        var relativePositionListRadius = 50;
        gridManager.RelativePositionList =
            new NativeArray<int2>(GridHelpers.CalculatePositionListLength(relativePositionListRadius), Allocator.Persistent);
        gridManager.RelativePositionRingInfoList = new NativeArray<int2>(relativePositionListRadius, Allocator.Persistent);
        gridManager.PopulateRelativePositionList(relativePositionListRadius);
    }
}
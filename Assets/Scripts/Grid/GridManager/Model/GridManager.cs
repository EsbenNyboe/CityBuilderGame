using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Grid
{
    public partial struct GridManager : IComponentData
    {
        public int Width;
        public int Height;
        public NativeArray<WalkableCell> WalkableGrid;
        public NativeArray<DamageableCell> DamageableGrid;
        public NativeArray<OccupiableCell> OccupiableGrid;
        public NativeArray<InteractableCell> InteractableGrid;

        public NativeArray<GridEntityType> GridEntityTypeGrid;
        public NativeArray<Entity> GridEntityGrid;

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
}
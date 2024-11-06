using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class GridSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(GridSystemGroup))]
public partial class SpawningSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SpawningSystemGroup))]
public partial class UnitBehaviourSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(UnitBehaviourSystemGroup))]
public partial class MovementSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MovementSystemGroup))]
public partial class AnimationSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AnimationSystemGroup))]
public partial class RenderingSystemGroup : ComponentSystemGroup
{
}
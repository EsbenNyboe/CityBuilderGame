using Unity.Entities;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class GridSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class SpawningSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class UnitBehaviourSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(UnitBehaviourSystemGroup))]
public partial class AnimationSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AnimationSystemGroup))]
public partial class RenderingSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
public partial class UnitStateSystemGroup : ComponentSystemGroup
{
}
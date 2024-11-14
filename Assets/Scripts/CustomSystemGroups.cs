using Unity.Entities;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class GridSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class LifetimeSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial class UnitStateSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class UnitBehaviourSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[UpdateBefore(typeof(UnitStateSystemGroup))]
public partial class PreRenderingSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class AnimationSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class RenderingSystemGroup : ComponentSystemGroup
{
}
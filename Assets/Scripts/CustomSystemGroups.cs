using Unity.Entities;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class GridSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class LifetimeSystemGroup : ComponentSystemGroup
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
public partial class PreRenderingSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
public partial class UnitStateSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class RenderingSystemGroup : ComponentSystemGroup
{
}
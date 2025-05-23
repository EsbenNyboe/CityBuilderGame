using Unity.Entities;

namespace SystemGroups
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class LifetimeSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class UnitBehaviourGridWritingSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class UnitStateSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(UnitStateSystemGroup))]
    public partial class PreRenderingSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class UnitBehaviourSystemGroup : ComponentSystemGroup
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
}
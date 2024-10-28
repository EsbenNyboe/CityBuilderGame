using Unity.Entities;
using Unity.Mathematics;

public struct TryDeoccupy : IComponentData
{
    public int2 NewTarget;
}

public struct TryOccupy : IComponentData
{
}
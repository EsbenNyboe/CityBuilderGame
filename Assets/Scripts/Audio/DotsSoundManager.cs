using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct DotsSoundManager : IComponentData
{
    public NativeQueue<float3> ChopSoundRequests;
    public NativeQueue<float3> DestroyTreeSoundRequests;
}
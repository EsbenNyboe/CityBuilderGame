using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct DotsSoundManager : IComponentData
{
    // TODO: Test what happens when enqueuing from multiple cores at the same time.
    public NativeQueue<float3> ChopSoundRequests;
    public NativeQueue<float3> DestroyTreeSoundRequests;
}
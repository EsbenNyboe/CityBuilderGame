using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Audio
{
    public struct DotsSoundManager : IComponentData
    {
        public NativeQueue<float3> ChopTreeSoundRequests;
        public NativeQueue<float3> DestroyTreeSoundRequests;
        public NativeQueue<float3> ChopBerryBushSoundRequests;
        public NativeQueue<float3> DestroyBerryBushSoundRequests;
    }
}
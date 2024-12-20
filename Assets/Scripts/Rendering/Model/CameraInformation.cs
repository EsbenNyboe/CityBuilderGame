using Unity.Entities;
using Unity.Mathematics;

namespace Rendering
{
    public struct CameraInformation : IComponentData
    {
        public float3 CameraPosition;
        public float OrthographicSize;
        public float ScreenRatio;
    }
}
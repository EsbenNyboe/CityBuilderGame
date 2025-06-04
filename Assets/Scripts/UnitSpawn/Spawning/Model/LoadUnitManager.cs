using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace UnitSpawn.Spawning
{
    public struct LoadUnitManager : IComponentData
    {
        public NativeList<float3> VillagersToLoad;
        public NativeList<float3> BoarsToLoad;

        public readonly bool HasUnits()
        {
            return VillagersToLoad is { IsEmpty: false, Length: > 0 }
                   || BoarsToLoad is { IsEmpty: false, Length: > 0 };
        }
    }
}
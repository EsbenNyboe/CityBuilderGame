using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(20)]
public struct PathPosition : IBufferElementData {

    public int2 position;

}

using Unity.Entities;

namespace GridEntityNS
{
    public struct Constructable : IComponentData
    {
        public int MaterialsRequired;
        public int Materials;
    }
}
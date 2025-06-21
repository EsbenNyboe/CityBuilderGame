using Unity.Entities;

namespace UnitBehaviours.AutonomousHarvesting.Model
{
    public struct CorpseRequest : IComponentData
    {
        public Entity RequesterEntity;
        public Entity CorpseEntity;
    }
}
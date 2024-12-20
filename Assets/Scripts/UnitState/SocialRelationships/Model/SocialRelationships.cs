using Unity.Collections;
using Unity.Entities;

namespace UnitState.SocialState
{
    public struct SocialRelationships : ICleanupComponentData
    {
        public NativeParallelHashMap<Entity, float> Relationships;
        public float TimeOfLastEvaluation;
    }
}
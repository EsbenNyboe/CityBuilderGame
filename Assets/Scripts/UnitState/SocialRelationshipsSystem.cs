using Unity.Collections;
using Unity.Entities;

namespace UnitState
{
    public struct SocialRelationships : IComponentData
    {
        public NativeHashMap<Entity, float> Relationships;
    }
    
    [UpdateInGroup(typeof(SpawningSystemGroup))]
    [UpdateAfter(typeof(SpawnManagerSystem))]
    [UpdateAfter(typeof(SpawnUnitsSystem))]
    public partial struct SocialRelationshipsSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            using var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            ecb.Playback(state.EntityManager);
        }
    }
}

using UnitAgency;
using Unity.Entities;
using Unity.Transforms;

namespace UnitBehaviours.Talking
{
    /// <summary>
    ///     Indicates that we are standing around and waiting for someone to talk to.
    /// </summary>
    public struct IsTalkative : IComponentData
    {
        public float Patience;
    }

    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsTalkativeSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem(typeof(GridManagerSystem));
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var isTalkativeLookup = SystemAPI.GetComponentLookup<IsTalkative>();
            var isTalkingLookup = SystemAPI.GetComponentLookup<IsTalking>();

            foreach (var (isTalkative, pathFollow, localTransform, entity) in SystemAPI
                         .Query<RefRW<IsTalkative>, RefRO<PathFollow>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                isTalkative.ValueRW.Patience -= SystemAPI.Time.DeltaTime;
                if (isTalkative.ValueRW.Patience <= 0)
                {
                    // I lost my patience...
                    ecb.RemoveComponent<IsTalkative>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }

                var cell = GridHelpers.GetXY(localTransform.ValueRO.Position);
                if (TalkingHelpers.TryGetNeighbourWithComponent(gridManager, cell, isTalkativeLookup, out _) ||
                    TalkingHelpers.TryGetNeighbourWithComponent(gridManager, cell, isTalkingLookup, out _))
                {
                    // I found someone to talk to!
                    ecb.RemoveComponent<IsTalkative>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                }
            }
        }
    }
}
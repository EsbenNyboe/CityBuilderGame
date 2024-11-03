using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace UnitAgency
{
    internal partial struct IsDecidingSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Following the example at: https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-entity-command-buffer-automatic-playback.html
            var commands = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<IsDeciding>>().WithEntityAccess())
            {
                commands.RemoveComponent<IsDeciding>(entity);
                DecideNextBehaviour(ref state, commands, entity);
            }

            commands.Playback(state.EntityManager);
        }

        private void DecideNextBehaviour(ref SystemState state, EntityCommandBuffer commands, Entity entity)
        {
            // TODO: Pass gridManager as argument
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var unitPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;

            // TODO: Move to Query
            var moodSleepiness = SystemAPI.GetComponent<MoodSleepiness>(entity);
            var isSleepy = moodSleepiness.Sleepiness > 0.2f;

            if (isSleepy)
            {
                if (gridManager.IsBed(unitPosition) && !gridManager.IsOccupied(unitPosition, entity))
                {
                    commands.AddComponent(entity, new IsSleeping());
                    gridManager.SetIsWalkable(unitPosition, false);
                }
                else
                {
                    commands.AddComponent(entity, new IsSeekingBed());
                }
            }
            else
            {
                commands.AddComponent(entity, new IsIdle());
                // commands.AddComponent(entity, new IsTickListener());
            }

            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
        }
    }
}

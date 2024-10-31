using Unity.Entities;
using Unity.Transforms;

namespace UnitAgency
{
    internal partial struct UnitAgencySystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Following the example at: https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/systems-entity-command-buffer-automatic-playback.html
            var ecbSystemSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var commands = ecbSystemSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, entity) in SystemAPI.Query<RefRO<IsDeciding>>().WithEntityAccess())
            {
                commands.RemoveComponent<IsDeciding>(entity);
                DecideNextBehaviour(ref state, commands, entity);
            }
        }

        private void DecideNextBehaviour(ref SystemState state, EntityCommandBuffer commands, Entity entity)
        {
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var unitPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;
            GridHelpers.GetXY(unitPosition, out var x, out var y);

            var moodSleepiness = SystemAPI.GetComponent<MoodSleepiness>(entity);
            var isSleepy = moodSleepiness.Sleepiness > 0.2f;

            if (isSleepy)
            {
                if (gridManager.IsInteractable(unitPosition) && !gridManager.IsInteractedWith(unitPosition))
                {
                    commands.AddComponent(entity, new IsSleeping());
                    gridManager.SetInteractor(unitPosition, entity);
                    gridManager.SetIsWalkable(unitPosition, false);
                }
                else
                {
                    commands.AddComponent(entity, new IsSeekingBed());
                }
            }
            else if (gridManager.GetInteractable(unitPosition) == entity)
            {
            }

            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
        }


        private bool SeekBed(ref SystemState state, EntityCommandBuffer commands, Entity entity)
        {
            // Tired... must find bed...
            commands.AddComponent<IsSeekingBed>(entity);
            return true;
        }
    }
}
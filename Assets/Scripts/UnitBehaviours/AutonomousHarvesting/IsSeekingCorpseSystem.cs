using Debugging;
using Grid;
using SystemGroups;
using UnitAgency.Data;
using UnitBehaviours.AutonomousHarvesting.Model;
using UnitBehaviours.Pathing;
using UnitBehaviours.Targeting.Core;
using UnitBehaviours.UnitManagers;
using UnitState.Mood;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial struct IsSeekingCorpseSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<QuadrantDataManager>();
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<DebugToggleManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var debugToggleManager = SystemAPI.GetSingleton<DebugToggleManager>();
            var isDebuggingPath = debugToggleManager.DebugPathfinding;

            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var jobHandleList = new NativeList<JobHandle>(Allocator.Temp);
            var quadrantDataManager = SystemAPI.GetSingleton<QuadrantDataManager>();
            var unitBehaviourManager = SystemAPI.GetSingleton<UnitBehaviourManager>();
            var ecb = GetEntityCommandBuffer(ref state);

            foreach (var (localTransform, pathFollow, moodInitiative, entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathFollow>, RefRW<MoodInitiative>>()
                         .WithAll<IsSeekingCorpse>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }

                var currentCell = GridHelpers.GetXY(localTransform.ValueRO.Position);
                if (gridManager.IsOccupied(currentCell, entity))
                {
                    continue;
                }

                // Are there any Corpses within my range?
                var foundCorpseInRange = QuadrantSystem.TryFindClosestEntity(quadrantDataManager.CorpseQuadrantMap, gridManager,
                    unitBehaviourManager.QuadrantSearchRange,
                    localTransform.ValueRO.Position, entity, out var closestTargetEntity, out var closestTargetDistance);

                if (!foundCorpseInRange)
                {
                    // I can't see any nearby Corpses
                    ecb.RemoveComponent<IsSeekingCorpse>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                // Am I adjacent to a Corpse?
                if (closestTargetDistance < 2)
                {
                    // I found my adjacent Corpse!
                    ecb.RemoveComponent<IsSeekingCorpse>(entity);
                    ecb.AddComponent<IsDeciding>(entity);
                    continue;
                }

                if (!moodInitiative.ValueRO.HasInitiative())
                {
                    continue;
                }

                moodInitiative.ValueRW.UseInitiative();

                var corpseTransform = SystemAPI.GetComponent<LocalTransform>(closestTargetEntity);
                var corpsePosition = corpseTransform.Position;
                var corpseCell = GridHelpers.GetXY(corpsePosition); // TODO: Replace this with "chopping cell"

                // I found a Corpse!! I will go there! 
                PathHelpers.TrySetPath(ecb, gridManager, entity, currentCell, corpseCell, isDebuggingPath);
            }

            state.Dependency = JobHandle.CombineDependencies(jobHandleList.AsArray());
            jobHandleList.Dispose();
        }

        [BurstCompile]
        private EntityCommandBuffer GetEntityCommandBuffer(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            return ecb.CreateCommandBuffer(state.WorldUnmanaged);
        }
    }
}
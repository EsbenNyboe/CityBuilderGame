using UnitAgency;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    [UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
    public partial class IsSeekingTreeSystem : SystemBase
    {
        private SystemHandle _gridManagerSystemHandle;

        protected override void OnCreate()
        {
            _gridManagerSystemHandle = World.GetExistingSystem<GridManagerSystem>();
        }

        protected override void OnUpdate()
        {
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var jobHandleList = new NativeList<JobHandle>(Allocator.Temp);

            foreach (var (localTransform, pathFollow, entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathFollow>>().WithAll<IsSeekingTree>()
                         .WithEntityAccess())
            {
                if (pathFollow.ValueRO.IsMoving())
                {
                    continue;
                }
                // I reacted my destination / I'm standing still: I should find a bed!

                // Am I adjacent to a tree?

                var ecb = GetEntityCommandBuffer();
                var currentCell = GridHelpers.GetXY(localTransform.ValueRO.Position);
                var job = new SeekTreeJob
                {
                    currentCell = currentCell,
                    entity = entity,
                    gridManager = gridManager,
                    ecb = ecb
                };
                jobHandleList.Add(job.Schedule());
            }

            JobHandle.CompleteAll(jobHandleList.AsArray());

            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
            jobHandleList.Dispose();
        }

        private EntityCommandBuffer GetEntityCommandBuffer()
        {
            var ecbTest = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            return ecbTest.CreateCommandBuffer(World.Unmanaged);
        }
    }

    [BurstCompile]
    public struct SeekTreeJob : IJob
    {
        [ReadOnly] public int2 currentCell;
        [ReadOnly] public Entity entity;
        [ReadOnly] public GridManager gridManager;
        public EntityCommandBuffer ecb;

        public void Execute()
        {
            if (gridManager.TryGetNeighbouringTreeCell(currentCell, out _))
            {
                // I found my adjacent tree! 
                ecb.RemoveComponent<IsSeekingTree>(entity);
                ecb.AddComponent<IsDeciding>(entity);
                return;
            }

            // I'm not next to a tree... I should find the closest tree.
            if (!gridManager.TryGetClosestChoppingCellSemiRandom(currentCell, entity, out var choppingCell))
            {
                // I can't see any nearby trees
                ecb.RemoveComponent<IsSeekingTree>(entity);
                ecb.AddComponent<IsDeciding>(entity);
                return;
            }

            // I found a tree!! I will go there! 
            PathHelpers.TrySetPath(ecb, entity, currentCell, choppingCell);
        }
    }
}
using UnitAgency;
using UnitState;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    public partial struct IsHarvestingSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;
        private SystemHandle _soundManagerSystemHandle;
        private SystemHandle _chopAnimationManager;

        public void OnCreate(ref SystemState state)
        {
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
            _soundManagerSystemHandle = state.World.GetExistingSystem<DotsSoundManagerSystem>();
            _chopAnimationManager = state.World.GetExistingSystem<ChopAnimationManagerSystem>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commands = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var soundManager = SystemAPI.GetComponent<DotsSoundManager>(_soundManagerSystemHandle);
            var chopAnimationManager = SystemAPI.GetComponent<ChopAnimationManager>(_chopAnimationManager);

            foreach (var (isHarvesting, localTransform, inventory, entity)
                     in SystemAPI.Query<RefRW<IsHarvesting>, RefRO<LocalTransform>, RefRW<Inventory>>().WithEntityAccess())
            {
                if (!gridManager.IsDamageable(isHarvesting.ValueRO.Tree))
                {
                    commands.RemoveComponent<IsHarvesting>(entity);
                    commands.AddComponent(entity, new IsDeciding());
                    commands.RemoveComponent<ChopAnimationTag>(entity);
                    SystemAPI.SetComponent(entity, new SpriteTransform
                    {
                        Position = float3.zero,
                        Rotation = quaternion.identity
                    });
                    continue;
                }

                isHarvesting.ValueRW.TimeUntilNextChop -= SystemAPI.Time.DeltaTime;
                if (isHarvesting.ValueRO.TimeUntilNextChop <= 0)
                {
                    ChopTree(
                        ref state,
                        soundManager,
                        ref gridManager,
                        chopAnimationManager,
                        isHarvesting.ValueRO.Tree,
                        localTransform,
                        inventory);
                    isHarvesting.ValueRW.TimeUntilNextChop = chopAnimationManager.ChopDuration;
                }
            }

            commands.Playback(state.EntityManager);
            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
        }

        private void ChopTree(
            ref SystemState state,
            DotsSoundManager soundManager,
            ref GridManager gridManager,
            ChopAnimationManager chopAnimationManager,
            int2 treeCoords,
            RefRO<LocalTransform> localTransform,
            RefRW<Inventory> inventory
        )
        {
            var treeGridIndex = gridManager.GetIndex(treeCoords);
            gridManager.AddDamage(treeGridIndex, chopAnimationManager.DamagePerChop);
            var soundPosition = GridHelpers.GetWorldPosition(treeCoords.x, treeCoords.y);
            soundManager.ChopSoundRequests.Enqueue(soundPosition);

            // If this damage I just did caused the tree's health to drop to zero...
            if (!gridManager.IsDamageable(treeGridIndex))
            {
                // I am the one who gets to take the log and destroys the tree
                inventory.ValueRW.CurrentItem = InventoryItem.LogOfWood;
                DestroyTree(ref state, soundManager, ref gridManager, treeCoords);
            }
        }

        private void DestroyTree(ref SystemState state,
            DotsSoundManager soundManager,
            ref GridManager gridManager,
            int2 tree
        )
        {
            var soundOrigin = GridHelpers.GetWorldPosition(tree.x, tree.y);
            soundManager.DestroyTreeSoundRequests.Enqueue(soundOrigin);
            gridManager.SetIsWalkable(tree.x, tree.y, true);
            gridManager.SetHealth(gridManager.GetIndex(tree), 0);
        }
    }
}

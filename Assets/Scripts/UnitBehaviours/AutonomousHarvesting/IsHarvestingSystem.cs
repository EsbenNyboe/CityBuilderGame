using UnitAgency;
using UnitState;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    public struct IsHarvesting : IComponentData
    {
        public readonly int2 Tree;
        public float TimeUntilNextChop;

        public IsHarvesting(int2 tree)
        {
            Tree = tree;
            TimeUntilNextChop = 0f;
        }
    }

    [UpdateInGroup(typeof(UnitBehaviourGridWritingSystemGroup))]
    public partial struct IsHarvestingSystem : ISystem
    {
        private SystemHandle _gridManagerSystemHandle;
        private SystemHandle _soundManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AttackAnimationManager>();
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _gridManagerSystemHandle = state.World.GetExistingSystem<GridManagerSystem>();
            _soundManagerSystemHandle = state.World.GetExistingSystem<DotsSoundManagerSystem>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var gridManager = SystemAPI.GetComponent<GridManager>(_gridManagerSystemHandle);
            var soundManager = SystemAPI.GetComponent<DotsSoundManager>(_soundManagerSystemHandle);
            var unitBehaviourManager = SystemAPI.GetSingleton<UnitBehaviourManager>();
            var attackAnimationManager = SystemAPI.GetSingleton<AttackAnimationManager>();

            foreach (var (isHarvesting, localTransform, inventory, entity)
                     in SystemAPI.Query<RefRW<IsHarvesting>, RefRO<LocalTransform>, RefRW<Inventory>>()
                         .WithEntityAccess())
            {
                if (!gridManager.IsDamageable(isHarvesting.ValueRO.Tree))
                {
                    ecb.RemoveComponent<IsHarvesting>(entity);
                    ecb.AddComponent(entity, new IsDeciding());
                    ecb.RemoveComponent<AttackAnimation>(entity);
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
                    var socialEventEntity = ecb.CreateEntity();
                    ecb.AddComponent(socialEventEntity, new SocialEvent
                    {
                        Perpetrator = entity,
                        Position = localTransform.ValueRO.Position,
                        InfluenceAmount = 0.1f,
                        InfluenceRadius = 3
                    });

                    ChopTree(ref state,
                        soundManager,
                        ref gridManager,
                        unitBehaviourManager,
                        isHarvesting.ValueRO.Tree,
                        localTransform,
                        inventory);
                    isHarvesting.ValueRW.TimeUntilNextChop = attackAnimationManager.AttackDuration;
                }
            }

            SystemAPI.SetComponent(_gridManagerSystemHandle, gridManager);
        }

        private void ChopTree(ref SystemState state,
            DotsSoundManager soundManager,
            ref GridManager gridManager,
            UnitBehaviourManager unitBehaviourManager,
            int2 treeCoords,
            RefRO<LocalTransform> localTransform,
            RefRW<Inventory> inventory)
        {
            var treeGridIndex = gridManager.GetIndex(treeCoords);
            gridManager.AddDamage(treeGridIndex, unitBehaviourManager.DamagePerChop);
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
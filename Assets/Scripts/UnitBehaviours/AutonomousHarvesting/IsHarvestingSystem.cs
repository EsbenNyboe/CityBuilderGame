﻿using UnitAgency;
using UnitState;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    public struct IsHarvesting : IComponentData
    {
    }

    [UpdateInGroup(typeof(UnitBehaviourGridWritingSystemGroup))]
    public partial struct IsHarvestingSystem : ISystem
    {
        private SystemHandle _soundManagerSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _soundManagerSystemHandle = state.World.GetExistingSystem<DotsSoundManagerSystem>();
            state.RequireForUpdate<AttackAnimationManager>();
            state.RequireForUpdate<UnitBehaviourManager>();
            state.RequireForUpdate<SocialDynamicsManager>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var soundManager = SystemAPI.GetComponent<DotsSoundManager>(_soundManagerSystemHandle);
            var attackAnimationManager = SystemAPI.GetSingleton<AttackAnimationManager>();
            var unitBehaviourManager = SystemAPI.GetSingleton<UnitBehaviourManager>();
            var socialDynamicsManager = SystemAPI.GetSingleton<SocialDynamicsManager>();

            foreach (var (attackAnimation, inventory, localTransform, entity)
                     in SystemAPI
                         .Query<RefRW<AttackAnimation>, RefRW<Inventory>, RefRO<LocalTransform>>()
                         .WithEntityAccess().WithAll<IsHarvesting>())
            {
                if (!gridManager.IsDamageable((int2)attackAnimation.ValueRO.Target))
                {
                    ecb.RemoveComponent<IsHarvesting>(entity);
                    attackAnimation.ValueRW.MarkedForDeletion = true;
                    ecb.AddComponent(entity, new IsDeciding());
                    continue;
                }

                if (attackAnimation.ValueRO.TimeLeft <= 0)
                {
                    var socialEventEntity = ecb.CreateEntity();
                    ecb.AddComponent(socialEventEntity, new SocialEvent
                    {
                        Perpetrator = entity,
                        Position = localTransform.ValueRO.Position,
                        InfluenceAmount = socialDynamicsManager.OnUnitAttackTree.InfluenceAmount,
                        InfluenceRadius = socialDynamicsManager.OnUnitAttackTree.InfluenceRadius
                    });

                    ChopTree(ref state,
                        soundManager,
                        ref gridManager,
                        unitBehaviourManager,
                        (int2)attackAnimation.ValueRO.Target,
                        inventory);
                    attackAnimation.ValueRW.TimeLeft = attackAnimationManager.AttackDuration;
                }
            }

            SystemAPI.SetSingleton(gridManager);
        }

        private void ChopTree(ref SystemState state,
            DotsSoundManager soundManager,
            ref GridManager gridManager,
            UnitBehaviourManager unitBehaviourManager,
            int2 treeCoords,
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
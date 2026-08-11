using System;
using Audio;
using Grid;
using Inventory;
using SpriteTransformNS;
using SystemGroups;
using UnitAgency.Data;
using UnitBehaviours.UnitManagers;
using UnitState.SocialState;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitBehaviours.AutonomousHarvesting
{
    // TODO: Need to create a system for harvesting berry bushes!
    [UpdateInGroup(typeof(UnitBehaviourGridWritingSystemGroup))]
    public partial struct IsHarvestingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DotsSoundManager>();
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
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
            var soundManager = SystemAPI.GetSingleton<DotsSoundManager>();
            var attackAnimationManager = SystemAPI.GetSingleton<AttackAnimationManager>();
            var unitBehaviourManager = SystemAPI.GetSingleton<UnitBehaviourManager>();
            var socialDynamicsManager = SystemAPI.GetSingleton<SocialDynamicsManager>();

            foreach (var (isHarvesting, attackAnimation, inventory, localTransform, entity)
                     in SystemAPI
                         .Query<RefRO<IsHarvesting>, RefRW<AttackAnimation>, RefRW<InventoryState>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                var target = (int2)attackAnimation.ValueRO.Target;
                if (inventory.ValueRO.CurrentItem != InventoryItem.None || !gridManager.IsDamageable(target))
                {
                    ecb.RemoveComponent<IsHarvesting>(entity);
                    attackAnimation.ValueRW.MarkedForDeletion = true;
                    ecb.AddComponent(entity, new IsDeciding());
                    continue;
                }

                if (attackAnimation.ValueRO.TimeLeft <= 0)
                {
                    var socialEventConfig = isHarvesting.ValueRO.HarvestableType switch
                    {
                        HarvestableType.Tree => socialDynamicsManager.OnUnitAttackTree,
                        HarvestableType.BerryBush => socialDynamicsManager.OnUnitAttackBerryBush,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    var socialEventEntity = ecb.CreateEntity();
                    ecb.AddComponent(socialEventEntity, new SocialEvent
                    {
                        Perpetrator = entity,
                        Position = localTransform.ValueRO.Position,
                        InfluenceAmount = socialEventConfig.InfluenceAmount,
                        InfluenceRadius = socialEventConfig.InfluenceRadius
                    });

                    Attack(ecb,
                        soundManager,
                        ref gridManager,
                        unitBehaviourManager,
                        isHarvesting.ValueRO.HarvestableType,
                        target,
                        inventory);
                    attackAnimation.ValueRW.TimeLeft = attackAnimationManager.AttackDuration;
                }
            }

            SystemAPI.SetSingleton(gridManager);
        }

        private void Attack(EntityCommandBuffer ecb,
            DotsSoundManager soundManager,
            ref GridManager gridManager,
            UnitBehaviourManager unitBehaviourManager,
            HarvestableType harvestableType,
            int2 harvestableCoords,
            RefRW<InventoryState> inventory)
        {
            var harvestableGridIndex = gridManager.GetIndex(harvestableCoords);
            gridManager.AddDamage(harvestableGridIndex, unitBehaviourManager.DamagePerChop);
            var soundPosition = GridHelpers.GetWorldPosition(harvestableCoords.x, harvestableCoords.y);
            switch (harvestableType)
            {
                case HarvestableType.Tree:
                    soundManager.ChopTreeSoundRequests.Enqueue(soundPosition);
                    break;
                case HarvestableType.BerryBush:
                    soundManager.ChopBerryBushSoundRequests.Enqueue(soundPosition);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(harvestableType), harvestableType, null);
            }

            // If this damage I just did caused the damageable's health to drop to zero...
            if (!gridManager.IsDamageable(harvestableGridIndex))
            {
                // I am the one who gets to take the loot and destroy the entity
                inventory.ValueRW.CurrentItem = harvestableType switch
                {
                    HarvestableType.Tree => InventoryItem.LogOfWood,
                    HarvestableType.BerryBush => InventoryItem.BunchOfBerries,
                    _ => throw new ArgumentOutOfRangeException(nameof(harvestableType), harvestableType, null)
                };
                DestroyHarvestable(ecb, soundManager, ref gridManager, harvestableCoords, harvestableType);
            }
            else if (gridManager.IsBerryBush(harvestableCoords))
            {
                // Everyone gets a berry, if they hit the berry bush!
                inventory.ValueRW.CurrentItem = InventoryItem.BunchOfBerries;
            }
        }

        private void DestroyHarvestable(EntityCommandBuffer ecb,
            DotsSoundManager soundManager,
            ref GridManager gridManager,
            int2 harvestableCell, HarvestableType harvestableType)
        {
            var soundOrigin = GridHelpers.GetWorldPosition(harvestableCell.x, harvestableCell.y);
            switch (harvestableType)
            {
                case HarvestableType.Tree:
                    soundManager.DestroyTreeSoundRequests.Enqueue(soundOrigin);
                    break;
                case HarvestableType.BerryBush:
                    soundManager.DestroyBerryBushSoundRequests.Enqueue(soundOrigin);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(harvestableType), harvestableType, null);
            }

            gridManager.SetIsWalkable(harvestableCell, true);
            gridManager.SetHealth(harvestableCell, 0);

            Entity harvestableEntity;
            var foundGridEntity = harvestableType switch
            {
                HarvestableType.Tree => gridManager.TryGetTreeEntity(harvestableCell, out harvestableEntity),
                HarvestableType.BerryBush => gridManager.TryGetBerryBushEntity(harvestableCell, out harvestableEntity),
                _ => throw new ArgumentOutOfRangeException(nameof(harvestableType), harvestableType, null)
            };

            if (foundGridEntity)
            {
                gridManager.RemoveGridEntity(harvestableCell);
                ecb.DestroyEntity(harvestableEntity);
            }
            else
            {
                Debug.LogError("There is no harvetable!");
            }
        }
    }
}
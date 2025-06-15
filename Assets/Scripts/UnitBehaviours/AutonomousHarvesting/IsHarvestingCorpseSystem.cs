using Audio;
using Grid;
using Inventory;
using SpriteTransformNS;
using SystemGroups;
using UnitAgency.Data;
using UnitBehaviours.AutonomousHarvesting.Model;
using UnitBehaviours.Targeting;
using UnitBehaviours.Targeting.Core;
using UnitBehaviours.UnitManagers;
using UnitState.Dead;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    [UpdateInGroup(typeof(UnitBehaviourGridWritingSystemGroup))]
    public partial struct IsHarvestingCorpseSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<QuadrantDataManager>();
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
            var quadrantDataManager = SystemAPI.GetSingleton<QuadrantDataManager>();

            foreach (var (isHarvestingCorpse, attackAnimation, inventory, localTransform, entity)
                     in SystemAPI
                         .Query<RefRO<IsHarvestingCorpse>, RefRW<AttackAnimation>, RefRW<InventoryState>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                if (inventory.ValueRO.CurrentItem != InventoryItem.None)
                {
                    ecb.RemoveComponent<IsHarvestingCorpse>(entity);
                    attackAnimation.ValueRW.MarkedForDeletion = true;
                    ecb.AddComponent(entity, new IsDeciding());
                    continue;
                }

                var corpseEntity = isHarvestingCorpse.ValueRO.Target;
                if (!SystemAPI.Exists(corpseEntity))
                {
                    ecb.RemoveComponent<IsHarvestingCorpse>(entity);
                    attackAnimation.ValueRW.MarkedForDeletion = true;
                    ecb.AddComponent(entity, new IsDeciding());
                    continue;
                }

                var corpseHealth = SystemAPI.GetComponent<Health>(corpseEntity);
                if (corpseHealth.CurrentHealth <= 0)
                {
                    ecb.RemoveComponent<IsHarvestingCorpse>(entity);
                    attackAnimation.ValueRW.MarkedForDeletion = true;
                    ecb.AddComponent(entity, new IsDeciding());
                    continue;
                }

                if (attackAnimation.ValueRO.TimeLeft <= 0)
                {
                    corpseHealth.CurrentHealth -= unitBehaviourManager.DamagePerAttack;
                    SystemAPI.SetComponent(corpseEntity, corpseHealth);
                    var corpse = SystemAPI.GetComponent<Corpse>(corpseEntity);
                    var currentCorpseContent = (float)corpse.MeatCurrent / corpse.MeatMax;
                    var maxHealth = corpseHealth.MaxHealth;
                    if (corpseHealth.CurrentHealth < currentCorpseContent * maxHealth)
                    {
                        // I will attempt to grab meat from the corpse
                        ecb.AddComponent(ecb.CreateEntity(), new CorpseRequest
                        {
                            RequesterEntity = entity,
                            CorpseEntity = corpseEntity
                        });
                    }

                    attackAnimation.ValueRW.TimeLeft = attackAnimationManager.AttackDuration;
                }
            }

            SystemAPI.SetSingleton(gridManager);
        }
    }
}
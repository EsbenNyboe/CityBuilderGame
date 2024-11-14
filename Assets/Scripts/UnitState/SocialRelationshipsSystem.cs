﻿using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitState
{
    public struct SocialRelationships : IComponentData
    {
        public NativeHashMap<Entity, float> Relationships;
    }

    [UpdateInGroup(typeof(LifetimeSystemGroup))]
    public partial struct SocialRelationshipsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        private const int InitialCapacity = 100;

        public void OnUpdate(ref SystemState state)
        {
            // SETUP NEW SOCIAL RELATIONSHIPS
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (_, spawnedEntity) in SystemAPI.Query<RefRO<SpawnedUnit>>().WithEntityAccess())
            {
                var relationships = new NativeHashMap<Entity, float>(InitialCapacity, Allocator.Persistent);
                foreach (var (existingSocialRelationships, existingEntity) in SystemAPI
                             .Query<RefRW<SocialRelationships>>().WithEntityAccess())
                {
                    relationships.Add(existingEntity, 0);
                    existingSocialRelationships.ValueRW.Relationships.Add(spawnedEntity, 0);
                }

                foreach (var (_, otherSpawnedEntity) in SystemAPI.Query<RefRO<SpawnedUnit>>().WithEntityAccess())
                {
                    relationships.Add(otherSpawnedEntity, 0);
                }

                var socialRelationships = new SocialRelationships
                {
                    Relationships = relationships
                };
                ecb.AddComponent(spawnedEntity, socialRelationships);
            }
        }
    }

    public struct SocialRelationshipsDebugSystemData : IComponentData
    {
        public bool DrawRelations;
    }

    public partial struct SocialRelationshipsDebugSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<SocialRelationshipsDebugSystemData>(state.SystemHandle);
        }

        public void OnUpdate(ref SystemState state)
        {
            var settings =
                state.EntityManager.GetComponentDataRW<SocialRelationshipsDebugSystemData>(state.SystemHandle);

            if (Input.GetKeyDown(KeyCode.KeypadMultiply))
            {
                settings.ValueRW.DrawRelations = !settings.ValueRO.DrawRelations;
            }

            if (!settings.ValueRO.DrawRelations)
            {
                return;
            }

            foreach (var (localTransform, socialRelationships) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<SocialRelationships>>().WithAll<UnitSelection>())
            {
                foreach (var relationship in socialRelationships.ValueRO.Relationships)
                {
                    var otherPosition = SystemAPI.GetComponent<LocalTransform>(relationship.Key).Position;
                    var position = localTransform.ValueRO.Position;
                    var direction = math.normalize(otherPosition - position);
                    var cross = math.cross(direction, new float3(0, 0, 0.1f));
                    Debug.DrawLine(position + cross, otherPosition + cross, GetRelationshipColor(relationship.Value));
                }
            }

            var mutualFondnessIncrement = 0f;
            if (Input.GetKey(KeyCode.KeypadPlus))
            {
                mutualFondnessIncrement += SystemAPI.Time.DeltaTime;
            }

            if (Input.GetKey(KeyCode.KeypadMinus))
            {
                mutualFondnessIncrement -= SystemAPI.Time.DeltaTime;
            }

            if (mutualFondnessIncrement != 0f)
            {
                foreach (var socialRelationships in SystemAPI.Query<RefRW<SocialRelationships>>()
                             .WithAll<UnitSelection>())
                {
                    foreach (var (_, otherEntity) in SystemAPI.Query<RefRO<UnitSelection>>().WithEntityAccess())
                    {
                        socialRelationships.ValueRW.Relationships[otherEntity] += mutualFondnessIncrement;
                    }
                }
            }
        }

        private Color GetRelationshipColor(float relationshipValue)
        {
            return Color.Lerp(
                new Color(0.5f, 0.5f, 0.5f, 0f),
                relationshipValue > 0 ? Color.green : Color.red,
                math.abs(relationshipValue));
        }
    }
}
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitState
{
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

        [BurstCompile]
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
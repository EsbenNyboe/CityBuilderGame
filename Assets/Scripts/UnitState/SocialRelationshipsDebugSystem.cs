using Debugging;
using Grid;
using UnitBehaviours.Tags;
using UnitState.SocialState;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitState
{
    public partial struct SocialRelationshipsDebugSystem : ISystem
    {
        private EntityQuery _selectedUnitsQuery;
        private EntityQuery _allUnitsQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SocialDebugManager>();
            _selectedUnitsQuery = state.GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<SocialRelationships>(), ComponentType.ReadOnly<UnitSelection>());
            _allUnitsQuery = state.GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<SocialRelationships>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var socialDebugManager = SystemAPI.GetSingleton<SocialDebugManager>();
            if (!socialDebugManager.DrawRelations)
            {
                return;
            }

            ManipulateRelationsLogic(ref state);

            var entities = socialDebugManager.IncludeNonSelections
                ? _allUnitsQuery.ToEntityArray(Allocator.TempJob)
                : _selectedUnitsQuery.ToEntityArray(Allocator.TempJob);
            var drawRelationsJob = new DrawRelationsJob
            {
                Entities = entities,
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                SocialRelationshipsLookup = SystemAPI.GetComponentLookup<SocialRelationships>(),
                SocialDebugManager = socialDebugManager
            };
            var jobHandle = drawRelationsJob.Schedule(entities.Length, 1);
            jobHandle.Complete();
            entities.Dispose();
        }

        [BurstCompile]
        private struct DrawRelationsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity> Entities;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<SocialRelationships> SocialRelationshipsLookup;

            [ReadOnly] public SocialDebugManager SocialDebugManager;

            public void Execute(int index)
            {
                var socialRelationships = SocialRelationshipsLookup[Entities[index]];
                var applyFilter = SocialDebugManager.ApplyFilter;
                var minFondness = SocialDebugManager.FilterSetting.MinFondnessDrawn;
                var maxFondness = SocialDebugManager.FilterSetting.MaxFondnessDrawn;

                var position = LocalTransformLookup[Entities[index]].Position;
                foreach (var relationship in socialRelationships.Relationships)
                {
                    if (applyFilter && (relationship.Value < minFondness || relationship.Value > maxFondness))
                    {
                        continue;
                    }

                    var otherPosition = LocalTransformLookup[relationship.Key].Position;
                    var direction = math.normalize(otherPosition - position);
                    var cross = math.cross(direction, new float3(0, 0, 0.1f));
                    Debug.DrawLine(position + cross, otherPosition + cross, GetRelationshipColor(relationship.Value));
                }

                var relationshipToSelf = socialRelationships.Relationships[Entities[index]];

                DebugHelper.DebugDrawCell(GridHelpers.GetXY(position), GetRelationshipColor(relationshipToSelf));
                DebugHelper.DebugDrawCross(GridHelpers.GetXY(position), GetRelationshipColor(relationshipToSelf));
            }

            private static Color GetRelationshipColor(float relationshipValue)
            {
                return Color.Lerp(
                    new Color(0.5f, 0.5f, 0.5f, 0f),
                    relationshipValue > 0 ? Color.green : Color.red,
                    math.abs(relationshipValue));
            }
        }

        private void ManipulateRelationsLogic(ref SystemState state)
        {
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
    }
}
using Events;
using Rendering;
using UnitState;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UnitBehaviours.Targeting
{
    public struct Spear : IComponentData
    {
        public float2 Direction;
        public float2 CurrentPosition;
        public int2 Target;
    }

    public partial struct SpearSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldSpriteSheetManager>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            _query = state.GetEntityQuery(typeof(Spear));
        }

        private const float SpearSpeed = 5f;
        private const float SpearDamageRadius = 0.5f;
        private const float SpearDamage = 10f;

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var worldSpriteSheetManager = SystemAPI.GetSingleton<WorldSpriteSheetManager>();
            var unitMesh = WorldSpriteSheetConfig.Instance.UnitMesh;
            var unitMaterial = WorldSpriteSheetConfig.Instance.UnitMaterial;
            var entityCount = _query.CalculateEntityCount();
            var uvTemplate = new Vector4
            {
                x = worldSpriteSheetManager.ColumnScale,
                y = worldSpriteSheetManager.RowScale
            };
            var uvArray = new Vector4[entityCount];
            var matrix4X4Array = new Matrix4x4[entityCount];

            var index = 0;
            foreach (var (spear, entity) in SystemAPI.Query<RefRW<Spear>>().WithEntityAccess())
            {
                uvArray[index] = new Vector4
                {
                    x = worldSpriteSheetManager.ColumnScale,
                    y = worldSpriteSheetManager.RowScale,
                    z = worldSpriteSheetManager.ColumnScale * worldSpriteSheetManager.Entries[(int)WorldSpriteSheetEntryType.Spear].EntryColumns[0],
                    w = worldSpriteSheetManager.RowScale * worldSpriteSheetManager.Entries[(int)WorldSpriteSheetEntryType.Spear].EntryRows[0]
                };
                var position = spear.ValueRO.CurrentPosition;
                var angleInDegrees = spear.ValueRO.Direction.x > 0 ? 0f : 180f;
                matrix4X4Array[index] = Matrix4x4.TRS(new Vector3(position.x, position.y), quaternion.EulerZXY(0, math.PI / 180 * angleInDegrees, 0),
                    Vector3.one);
                index++;

                var distanceBeforeMoving = math.distance(position, spear.ValueRO.Target);
                spear.ValueRW.CurrentPosition += spear.ValueRO.Direction * SystemAPI.Time.DeltaTime * SpearSpeed;
                var distanceAfterMoving = math.distance(spear.ValueRO.CurrentPosition, spear.ValueRO.Target);
                if (distanceBeforeMoving < distanceAfterMoving)
                {
                    ecb.DestroyEntity(entity);

                    var spearPosition2D = spear.ValueRO.CurrentPosition;
                    var spearPosition = new float3(spearPosition2D.x, spearPosition2D.y, 0);

                    foreach (var (_, localTransform, health, boarEntity) in SystemAPI.Query
                                 <RefRO<Boar>, RefRO<LocalTransform>, RefRW<Health>>().WithEntityAccess())
                    {
                        var boarPosition = localTransform.ValueRO.Position;
                        if (math.distance(spearPosition, boarPosition) > SpearDamageRadius)
                        {
                            continue;
                        }

                        health.ValueRW.CurrentHealth -= SpearDamage;
                        if (health.ValueRO.CurrentHealth < 0)
                        {
                            SystemAPI.SetComponentEnabled<IsAlive>(boarEntity, false);
                        }
                        else
                        {
                            ecb.AddComponent(ecb.CreateEntity(), new DamageEvent
                            {
                                Position = spearPosition
                            });
                        }

                        break;
                    }
                }
            }

            if (entityCount > 0)
            {
                WorldSpriteSheetRendererSystem.DrawMesh(new MaterialPropertyBlock(), unitMesh, unitMaterial, uvArray, matrix4X4Array, uvArray.Length);
            }
        }
    }
}
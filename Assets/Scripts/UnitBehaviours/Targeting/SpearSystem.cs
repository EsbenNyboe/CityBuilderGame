using Rendering;
using Unity.Entities;
using Unity.Mathematics;
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
                }
            }

            if (entityCount > 0)
            {
                WorldSpriteSheetRendererSystem.DrawMesh(new MaterialPropertyBlock(), unitMesh, unitMaterial, uvArray, matrix4X4Array, uvArray.Length);
            }
        }
    }
}
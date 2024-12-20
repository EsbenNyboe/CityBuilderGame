using Rendering;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace UnitBehaviours.Targeting
{
    public partial struct SpearRenderingSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldSpriteSheetManager>();
            _query = state.GetEntityQuery(typeof(Spear));
        }

        public void OnUpdate(ref SystemState state)
        {
            var entityCount = _query.CalculateEntityCount();
            if (entityCount <= 0)
            {
                return;
            }

            var uvArray = new Vector4[entityCount];
            var matrix4X4Array = new Matrix4x4[entityCount];
            var worldSpriteSheetManager = SystemAPI.GetSingleton<WorldSpriteSheetManager>();

            var index = 0;
            foreach (var (spear, entity) in SystemAPI.Query<RefRO<Spear>>().WithEntityAccess())
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
            }

            WorldSpriteSheetRendererSystem.DrawMesh(new MaterialPropertyBlock(),
                WorldSpriteSheetConfig.Instance.UnitMesh,
                WorldSpriteSheetConfig.Instance.UnitMaterial,
                uvArray, matrix4X4Array, uvArray.Length);
        }
    }
}
using CodeMonkey.Utils;
using Debugging;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace UnitBehaviours.Targeting
{
    public partial struct QuadrantSystemDebuggingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DebugToggleManager>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var isDebugging = SystemAPI.GetSingleton<DebugToggleManager>().DebugQuadrantSystem;
            if (isDebugging)
            {
                DebugDrawQuadrant(UtilsClass.GetMouseWorldPosition(), QuadrantSystem.QuadrantCellSize);
            }
        }

        private static void DebugDrawQuadrant(float3 position, float quadrantCellSize)
        {
            var lowerLeft = new Vector3(math.floor(position.x / quadrantCellSize) * quadrantCellSize,
                math.floor(position.y / quadrantCellSize) * quadrantCellSize, 0);
            Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(+1, +0) * quadrantCellSize);
            Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(+0, +1) * quadrantCellSize);
            Debug.DrawLine(lowerLeft + new Vector3(+1, +0) * quadrantCellSize,
                lowerLeft + new Vector3(+1, +1) * quadrantCellSize);
            Debug.DrawLine(lowerLeft + new Vector3(+0, +1) * quadrantCellSize,
                lowerLeft + new Vector3(+1, +1) * quadrantCellSize);
        }
    }
}
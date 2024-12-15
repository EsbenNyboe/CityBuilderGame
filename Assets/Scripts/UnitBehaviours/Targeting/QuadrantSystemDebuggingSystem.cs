using CodeMonkey.Utils;
using Debugging;
using Grid;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace UnitBehaviours.Targeting
{
    public struct QuadrantSystemDebugging : IComponentData
    {
        public int Index;
    }

    public partial struct QuadrantSystemDebuggingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridManager>();
            state.RequireForUpdate<QuadrantSystemDebugging>();
            state.RequireForUpdate<DebugToggleManager>();
            state.EntityManager.CreateSingleton<QuadrantSystemDebugging>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var isDebugging = SystemAPI.GetSingleton<DebugToggleManager>().DebugQuadrantSystem;
            if (!isDebugging)
            {
                return;
            }

            var quadrantSystemDebugging = SystemAPI.GetSingleton<QuadrantSystemDebugging>();
            var length = 100;

            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                quadrantSystemDebugging.Index++;
                if (quadrantSystemDebugging.Index >= length)
                {
                    quadrantSystemDebugging.Index = 0;
                }

                SystemAPI.SetSingleton(quadrantSystemDebugging);
            }

            var position = UtilsClass.GetMouseWorldPosition();
            var hashMapKey = QuadrantSystem.GetHashMapKeyFromPosition(position);
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var quadrantIndex = quadrantSystemDebugging.Index;
            var relativePositionIndexes = gridManager.RelativePositionList[quadrantIndex];
            var relativeHashmap = relativePositionIndexes.x + relativePositionIndexes.y * QuadrantSystem.QuadrantYMultiplier;

            DebugDrawQuadrant(hashMapKey + relativeHashmap);
        }

        private static void DebugDrawQuadrant(int hashMapKey)
        {
            var position = QuadrantSystem.GetQuadrantCenterPositionFromHashMapKey(hashMapKey);
            DebugDrawQuadrant(position, QuadrantSystem.QuadrantCellSize);
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
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Debugging
{
    public struct DebugPopupEvent : IComponentData
    {
        public DebugPopupEventType Type;
        public int2 Cell;

        public bool IsInitialized;
        public float TimeWhenCreated;
    }

    public enum DebugPopupEventType
    {
        None,
        SleepOccupancyIssue,
        PathNotFoundStart,
        PathNotFoundEnd
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class DebugPopupEventSystem : SystemBase
    {
        private const float PopupLifetime = 3f;

        protected override void OnUpdate()
        {
            var ecb = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(World.Unmanaged);
            var currentTime = SystemAPI.Time.ElapsedTime;
            foreach (var (debugPopupEvent, entity) in SystemAPI.Query<RefRW<DebugPopupEvent>>().WithEntityAccess())
            {
                var debugColor = GetDebugColor(debugPopupEvent.ValueRO.Type);
                
                if (!debugPopupEvent.ValueRO.IsInitialized)
                {
                    debugPopupEvent.ValueRW.IsInitialized = true;
                    debugPopupEvent.ValueRW.TimeWhenCreated = (float)currentTime;
                    DebugPopupEventManager.Instance.ShowPopup(
                        GridHelpers.GetWorldPosition(debugPopupEvent.ValueRO.Cell), PopupLifetime,
                        debugPopupEvent.ValueRO.Type, debugColor);
                }

                DebugDrawCell(debugPopupEvent.ValueRO.Cell, debugColor);
                if (currentTime > debugPopupEvent.ValueRO.TimeWhenCreated + PopupLifetime)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }

        private static void DebugDrawCell(int2 cell, Color color)
        {
            var padding = 0f;
            var offset = 1f - padding;
            var debugPosition = new Vector3(cell.x - 0.5f + padding, cell.y - 0.5f + padding, 0);
            Debug.DrawLine(debugPosition,
                debugPosition + new Vector3(+offset, +0), color);
            Debug.DrawLine(debugPosition,
                debugPosition + new Vector3(+0, +offset), color);
            Debug.DrawLine(debugPosition + new Vector3(+offset, +0),
                debugPosition + new Vector3(+offset, +offset), color);
            Debug.DrawLine(debugPosition + new Vector3(+0, +offset),
                debugPosition + new Vector3(+offset, +offset), color);
        }

        private static Color GetDebugColor(DebugPopupEventType debugPopupEventType)
        {
            return debugPopupEventType switch
            {
                DebugPopupEventType.PathNotFoundStart => Color.magenta,
                DebugPopupEventType.PathNotFoundEnd => Color.yellow,
                _ => Color.red
            };
        }
    }
}
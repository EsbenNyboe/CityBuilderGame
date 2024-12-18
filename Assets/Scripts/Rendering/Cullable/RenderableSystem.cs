using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Rendering.Cullable
{
    public struct Renderable : IComponentData, IEnableableComponent
    {
    }

    public partial struct RenderableSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<CameraInformation>();
            _query = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(LocalTransform)
                },
                Present = new ComponentType[]
                {
                    typeof(Renderable)
                }
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            GetCameraBounds(ref state, out var yTop, out var yBottom, out var xLeft, out var xRight);

            new CullJob
            {
                EcbParallelWriter = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                XLeft = xLeft,
                XRight = xRight,
                YTop = yTop,
                YBottom = yBottom
            }.ScheduleParallel(_query, state.Dependency).Complete();
        }

        [BurstCompile]
        private partial struct CullJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter EcbParallelWriter;

            [ReadOnly] public float XLeft; // Left most cull position
            [ReadOnly] public float XRight; // Right most cull position
            [ReadOnly] public float YTop; // Top most cull position
            [ReadOnly] public float YBottom; // Bottom most cull position

            public void Execute(in Entity entity, in LocalTransform localTransform, [EntityIndexInChunk] int index)
            {
                var isVisible = IsWithinCameraBounds(localTransform);
                EcbParallelWriter.SetComponentEnabled<Renderable>(index, entity, isVisible);
            }

            private bool IsWithinCameraBounds(LocalTransform localTransform)
            {
                var positionX = localTransform.Position.x;
                if (!(positionX > XLeft) || !(positionX < XRight))
                {
                    // Unit is not within horizontal view-bounds. No need to render.
                    return false;
                }

                var positionY = localTransform.Position.y;
                if (!(positionY > YBottom) || !(positionY < YTop))
                {
                    // Unit is not within vertical view-bounds. No need to render.
                    return false;
                }

                return true;
            }
        }

        private void GetCameraBounds(ref SystemState state, out float yTop, out float yBottom, out float xLeft, out float xRight)
        {
            var cameraInformation = SystemAPI.GetSingleton<CameraInformation>();
            var cameraPosition = cameraInformation.CameraPosition;
            var screenRatio = cameraInformation.ScreenRatio;
            var orthographicSize = cameraInformation.OrthographicSize;

            var cullBuffer = 1f; // We add some buffer, so culling is not noticable
            var cameraSizeX = orthographicSize * screenRatio + cullBuffer;
            var cameraSizeY = orthographicSize + cullBuffer;

            xLeft = cameraPosition.x - cameraSizeX;
            xRight = cameraPosition.x + cameraSizeX;
            yTop = cameraPosition.y + cameraSizeY;
            yBottom = cameraPosition.y - cameraSizeY;
        }
    }
}
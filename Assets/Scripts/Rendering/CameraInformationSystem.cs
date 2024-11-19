using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Rendering
{
    public struct CameraInformation : IComponentData
    {
        public float3 CameraPosition;
        public float OrthographicSize;
        public float ScreenRatio;
    }

    public partial struct CameraInformationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.CreateSingleton<CameraInformation>();
            state.RequireForUpdate<CameraInformation>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("Camera is null");
                throw new Exception();
            }

            var cameraInformation = new CameraInformation
            {
                CameraPosition = camera.transform.position,
                OrthographicSize = camera.orthographicSize,
                ScreenRatio = Screen.width / (float)Screen.height
            };
            var singletonEntity = SystemAPI.GetSingletonEntity<CameraInformation>();
            SystemAPI.SetComponent(singletonEntity, cameraInformation);
        }
    }
}
using System.Collections.Generic;
using CodeMonkey.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct UnitSelection : IComponentData
{
}

public partial class UnitControlSystem : SystemBase
{
    private float3 startPosition;

    protected override void OnUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Mouse pressed
            startPosition = UtilsClass.GetMouseWorldPosition();
            SelectionAreaManager.Instance.SelectionArea.gameObject.SetActive(true);
            SelectionAreaManager.Instance.SelectionArea.position = startPosition;
        }

        if (Input.GetMouseButton(0))
        {
            // Mouse held down
            var selectionAreaSize = (float3)UtilsClass.GetMouseWorldPosition() - startPosition;
            SelectionAreaManager.Instance.SelectionArea.localScale = selectionAreaSize;
        }

        if (Input.GetMouseButtonUp(0))
        {
            // Mouse released
            float3 endPosition = UtilsClass.GetMouseWorldPosition();
            SelectionAreaManager.Instance.SelectionArea.gameObject.SetActive(false);

            var lowerLeftPosition = new float3(math.min(startPosition.x, endPosition.x), math.min(startPosition.y, endPosition.y), 0);
            var upperRightPosition = new float3(math.max(startPosition.x, endPosition.x), math.max(startPosition.y, endPosition.y), 0);

            bool selectOnlyOneEntity = false;
            var selectionAreaSize = math.distance(lowerLeftPosition, upperRightPosition);
            var selectionAreaMinSize = 2f;
            if (selectionAreaSize < selectionAreaMinSize)
            {
                lowerLeftPosition += new float3(-1, -1, 0) * (selectionAreaMinSize - selectionAreaSize) * 0.5f;
                upperRightPosition += new float3(1, 1, 0) * (selectionAreaMinSize - selectionAreaSize) * 0.5f;
                selectOnlyOneEntity = true;
            }


            var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<UnitSelection>>().WithEntityAccess())
            {
                entityCommandBuffer.RemoveComponent(entity, typeof(UnitSelection));
            }

            int selectedEntityCount = 0;
            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (selectOnlyOneEntity && selectedEntityCount > 0)
                {
                    continue;
                }

                var entityPosition = localTransform.ValueRO.Position;
                if (entityPosition.x >= lowerLeftPosition.x &&
                    entityPosition.y >= lowerLeftPosition.y &&
                    entityPosition.x <= upperRightPosition.x &&
                    entityPosition.y <= upperRightPosition.y)
                {
                    entityCommandBuffer.AddComponent(entity, new UnitSelection());
                    selectedEntityCount++;
                }
            }

            entityCommandBuffer.Playback(EntityManager);
        }

        if (Input.GetMouseButtonDown(1) && !Input.GetKey(KeyCode.LeftControl))
        {
            // Right mouse button down
            float3 targetPosition = UtilsClass.GetMouseWorldPosition();
            List<float3> movePositionList = GetPositionListAround(targetPosition,new float[]{1f, 2f, 3f}, new int[]{5, 10, 20});
            int positionIndex = 0;
            foreach (var (unitSelection, moveTo, entity) in SystemAPI.Query<RefRO<UnitSelection>, RefRW<MoveTo>>().WithEntityAccess())
            {
                moveTo.ValueRW.Position = movePositionList[positionIndex];
                positionIndex = (positionIndex + 1) % movePositionList.Count; 
                moveTo.ValueRW.Move = true;
            }
        }
    }

    private List<float3> GetPositionListAround(float3 startPosition, float[] ringDistance, int[] ringPositionCount)
    {
        List<float3> positionList = new List<float3>();
        positionList.Add(startPosition);
        for (int ring = 0; ring < ringPositionCount.Length; ring++)
        {
            List<float3> ringPositionList =
                GetPositionListAround(startPosition, ringDistance[ring], ringPositionCount[ring]);
            positionList.AddRange(ringPositionList);
        }

        return positionList;
    }

    private List<float3> GetPositionListAround(float3 startPosition, float distance, int positionCount)
    {
        List<float3> positionList = new List<float3>();
        positionList.Add(startPosition);
        for (int i = 0; i < positionCount; i++)
        {
            int angle = i * (360 / positionCount);
            float3 dir = ApplyRotationToVector(new float3(0, 1, 0), angle);
            float3 position = startPosition + dir * distance;
            positionList.Add(position);
        }

        return positionList;
    }

    private float3 ApplyRotationToVector(float3 vec, float angle)
    {
        return Quaternion.Euler(0, 0, angle) * vec;
    }
}
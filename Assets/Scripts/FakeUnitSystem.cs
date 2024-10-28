using Unity.Burst;
using Unity.Entities;
using UnityEngine;

public struct FakeUnitManager : IComponentData
{
    public bool HeavinessIsEnabled;
}

public partial struct FakeUnitSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.EntityManager.AddComponent<FakeUnitManager>(state.SystemHandle);
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityCommandBuffer = new EntityCommandBuffer(state.WorldUpdateAllocator);
        var fakeUnitManager = SystemAPI.GetComponent<FakeUnitManager>(state.SystemHandle);
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            fakeUnitManager.HeavinessIsEnabled = true;
            SystemAPI.SetComponent(state.SystemHandle, fakeUnitManager);
            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithEntityAccess())
            {
                SystemAPI.SetComponentEnabled<FakeUnitTag2>(VARIABLE.Item2, true);
                SystemAPI.SetComponentEnabled<FakeUnitTag3>(VARIABLE.Item2, true);
                SystemAPI.SetComponentEnabled<FakeUnitTag4>(VARIABLE.Item2, true);
                SystemAPI.SetComponentEnabled<FakeUnitTag5>(VARIABLE.Item2, true);
            }
        }

        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            fakeUnitManager.HeavinessIsEnabled = false;
            SystemAPI.SetComponent(state.SystemHandle, fakeUnitManager);
            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithEntityAccess())
            {
                SystemAPI.SetComponentEnabled<FakeUnitTag2>(VARIABLE.Item2, false);
                SystemAPI.SetComponentEnabled<FakeUnitTag3>(VARIABLE.Item2, false);
                SystemAPI.SetComponentEnabled<FakeUnitTag4>(VARIABLE.Item2, false);
                SystemAPI.SetComponentEnabled<FakeUnitTag5>(VARIABLE.Item2, false);
            }
        }

        entityCommandBuffer.Playback(state.EntityManager);

        foreach (var fakeUnit in SystemAPI
                     .Query<RefRO<FakeUnitTag1>, RefRO<FakeUnitTag2>, RefRO<FakeUnitTag3>, RefRO<FakeUnitTag4>, RefRO<FakeUnitTag5>>())
        {
        }

        return;
        if (!fakeUnitManager.HeavinessIsEnabled)
        {
            foreach (var fakeUnit in SystemAPI
                         .Query<RefRO<FakeUnitTag1>>())
            {
            }
        }
        else
        {
            foreach (var fakeUnit in SystemAPI
                         .Query<RefRO<FakeUnitTag1>, RefRO<FakeUnitTag2>, RefRO<FakeUnitTag3>, RefRO<FakeUnitTag4>, RefRO<FakeUnitTag5>>())
            {
            }
        }
    }
}
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
            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithEntityAccess())
            {
                SystemAPI.SetComponentEnabled<FakeUnitTag2>(VARIABLE.Item2, false);
                SystemAPI.SetComponentEnabled<FakeUnitTag3>(VARIABLE.Item2, false);
                SystemAPI.SetComponentEnabled<FakeUnitTag4>(VARIABLE.Item2, false);
                SystemAPI.SetComponentEnabled<FakeUnitTag5>(VARIABLE.Item2, false);
            }
        }

        entityCommandBuffer.Playback(state.EntityManager);

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            fakeUnitManager.HeavinessIsEnabled = true;
            SystemAPI.SetComponent(state.SystemHandle, fakeUnitManager);
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            fakeUnitManager.HeavinessIsEnabled = false;
            SystemAPI.SetComponent(state.SystemHandle, fakeUnitManager);
        }

        if (!fakeUnitManager.HeavinessIsEnabled)
        {
            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithAll<FakeUnitTag2>().WithAll<FakeUnitTag3>().WithAll<FakeUnitTag4>()
                         .WithAll<FakeUnitTag5>().WithEntityAccess())
            {
                fakeUnitManager.HeavinessIsEnabled = false;
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithAll<FakeUnitTag2>().WithAll<FakeUnitTag3>().WithAll<FakeUnitTag4>()
                         .WithAll<FakeUnitTag5>().WithEntityAccess())
            {
                fakeUnitManager.HeavinessIsEnabled = false;
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithAll<FakeUnitTag2>().WithAll<FakeUnitTag3>().WithAll<FakeUnitTag4>()
                         .WithAll<FakeUnitTag5>().WithEntityAccess())
            {
                fakeUnitManager.HeavinessIsEnabled = false;
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithAll<FakeUnitTag2>().WithAll<FakeUnitTag3>().WithAll<FakeUnitTag4>()
                         .WithAll<FakeUnitTag5>().WithEntityAccess())
            {
                fakeUnitManager.HeavinessIsEnabled = false;
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithAll<FakeUnitTag2>().WithAll<FakeUnitTag3>().WithAll<FakeUnitTag4>()
                         .WithAll<FakeUnitTag5>().WithEntityAccess())
            {
                fakeUnitManager.HeavinessIsEnabled = false;
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithAll<FakeUnitTag2>().WithAll<FakeUnitTag3>().WithAll<FakeUnitTag4>()
                         .WithAll<FakeUnitTag5>().WithEntityAccess())
            {
                fakeUnitManager.HeavinessIsEnabled = false;
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithAll<FakeUnitTag2>().WithAll<FakeUnitTag3>().WithAll<FakeUnitTag4>()
                         .WithAll<FakeUnitTag5>().WithEntityAccess())
            {
                fakeUnitManager.HeavinessIsEnabled = false;
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithAll<FakeUnitTag2>().WithAll<FakeUnitTag3>().WithAll<FakeUnitTag4>()
                         .WithAll<FakeUnitTag5>().WithEntityAccess())
            {
                fakeUnitManager.HeavinessIsEnabled = false;
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithAll<FakeUnitTag2>().WithAll<FakeUnitTag3>().WithAll<FakeUnitTag4>()
                         .WithAll<FakeUnitTag5>().WithEntityAccess())
            {
                fakeUnitManager.HeavinessIsEnabled = false;
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithAll<FakeUnitTag2>().WithAll<FakeUnitTag3>().WithAll<FakeUnitTag4>()
                         .WithAll<FakeUnitTag5>().WithEntityAccess())
            {
                fakeUnitManager.HeavinessIsEnabled = false;
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithAll<FakeUnitTag2>().WithAll<FakeUnitTag3>().WithAll<FakeUnitTag4>()
                         .WithAll<FakeUnitTag5>().WithEntityAccess())
            {
                fakeUnitManager.HeavinessIsEnabled = false;
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithAll<FakeUnitTag2>().WithAll<FakeUnitTag3>().WithAll<FakeUnitTag4>()
                         .WithAll<FakeUnitTag5>().WithEntityAccess())
            {
                fakeUnitManager.HeavinessIsEnabled = false;
            }
        }
        else
        {
            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithEntityAccess())
            {
                if (SystemAPI.IsComponentEnabled<FakeUnitTag2>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag3>(VARIABLE.Item2) &&
                    SystemAPI.IsComponentEnabled<FakeUnitTag4>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag5>(VARIABLE.Item2))
                {
                    fakeUnitManager.HeavinessIsEnabled = true;
                }
            }
        }

        var test = false;
        if (test)
        {
            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithEntityAccess())
            {
                if (SystemAPI.IsComponentEnabled<FakeUnitTag2>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag3>(VARIABLE.Item2) &&
                    SystemAPI.IsComponentEnabled<FakeUnitTag4>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag5>(VARIABLE.Item2))
                {
                    fakeUnitManager.HeavinessIsEnabled = true;
                }
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithEntityAccess())
            {
                if (SystemAPI.IsComponentEnabled<FakeUnitTag2>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag3>(VARIABLE.Item2) &&
                    SystemAPI.IsComponentEnabled<FakeUnitTag4>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag5>(VARIABLE.Item2))
                {
                    fakeUnitManager.HeavinessIsEnabled = true;
                }
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithEntityAccess())
            {
                if (SystemAPI.IsComponentEnabled<FakeUnitTag2>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag3>(VARIABLE.Item2) &&
                    SystemAPI.IsComponentEnabled<FakeUnitTag4>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag5>(VARIABLE.Item2))
                {
                    fakeUnitManager.HeavinessIsEnabled = true;
                }
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithEntityAccess())
            {
                if (SystemAPI.IsComponentEnabled<FakeUnitTag2>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag3>(VARIABLE.Item2) &&
                    SystemAPI.IsComponentEnabled<FakeUnitTag4>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag5>(VARIABLE.Item2))
                {
                    fakeUnitManager.HeavinessIsEnabled = true;
                }
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithEntityAccess())
            {
                if (SystemAPI.IsComponentEnabled<FakeUnitTag2>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag3>(VARIABLE.Item2) &&
                    SystemAPI.IsComponentEnabled<FakeUnitTag4>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag5>(VARIABLE.Item2))
                {
                    fakeUnitManager.HeavinessIsEnabled = true;
                }
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithEntityAccess())
            {
                if (SystemAPI.IsComponentEnabled<FakeUnitTag2>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag3>(VARIABLE.Item2) &&
                    SystemAPI.IsComponentEnabled<FakeUnitTag4>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag5>(VARIABLE.Item2))
                {
                    fakeUnitManager.HeavinessIsEnabled = true;
                }
            }

            foreach (var VARIABLE in SystemAPI.Query<RefRO<FakeUnitTag1>>().WithEntityAccess())
            {
                if (SystemAPI.IsComponentEnabled<FakeUnitTag2>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag3>(VARIABLE.Item2) &&
                    SystemAPI.IsComponentEnabled<FakeUnitTag4>(VARIABLE.Item2) && SystemAPI.IsComponentEnabled<FakeUnitTag5>(VARIABLE.Item2))
                {
                    fakeUnitManager.HeavinessIsEnabled = true;
                }
            }
        }
    }
}
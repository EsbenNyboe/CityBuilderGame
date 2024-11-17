using Unity.Entities;
using UnityEngine;

public struct DebugToggleManager : IComponentData
{
    public bool IsDebugging;
}

public partial struct DebugToggleManagerSystem : ISystem
{
    private const bool IsDebuggingDefault = true;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DebugToggleManager>();
        var singletonEntity = state.EntityManager.CreateSingleton<DebugToggleManager>();
        SystemAPI.SetComponent(singletonEntity, new DebugToggleManager
        {
            IsDebugging = IsDebuggingDefault
        });
    }

    public void OnUpdate(ref SystemState state)
    {
        var debugToggleManager = SystemAPI.GetSingletonRW<DebugToggleManager>();
        if (Input.GetKeyDown(KeyCode.End))
        {
            debugToggleManager.ValueRW.IsDebugging = !debugToggleManager.ValueRW.IsDebugging;
        }
    }
}
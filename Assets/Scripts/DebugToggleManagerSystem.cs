using Unity.Entities;
using UnityEngine;

public struct DebugToggleManager : IComponentData
{
    public bool IsDebugging;
}

public partial struct DebugToggleManagerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DebugToggleManager>();
        state.EntityManager.CreateSingleton<DebugToggleManager>();
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
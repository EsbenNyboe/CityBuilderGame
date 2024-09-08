using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class HarvestableSystem : SystemBase
{
    private const float DegradationPerSec = 20;
    protected override void OnUpdate()
    {
        var degradeEverything = Input.GetKey(KeyCode.A);
        var degradationThisFrame = DegradationPerSec * SystemAPI.Time.DeltaTime;

        foreach (var (unitDegradation, transform, entity) in SystemAPI.Query<RefRW<UnitDegradation>, RefRW<PostTransformMatrix>>().WithEntityAccess())
        {
            unitDegradation.ValueRW.IsDegrading = degradeEverything;
            if (!degradeEverything)
            {
                continue;
            }

            unitDegradation.ValueRW.Health -= degradationThisFrame;

            if (unitDegradation.ValueRW.Health > 0)
            {
                transform.ValueRW.Value = float4x4.Scale(1, unitDegradation.ValueRO.Health / unitDegradation.ValueRO.MaxHealth, 1);
            }
            else
            {
                Debug.Log("Is dead");
            }
        }
    }
}

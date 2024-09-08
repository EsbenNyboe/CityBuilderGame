using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class UnitDegradationAuthoring : MonoBehaviour
{
    [SerializeField] private float _maxHealth;
    public class Baker : Baker<UnitDegradationAuthoring>
    {
        public override void Bake(UnitDegradationAuthoring authoring)
        {
            var entity = GetEntity(authoring);
            AddComponent(entity, new UnitDegradation
            {
                Health = authoring._maxHealth,
                MaxHealth = authoring._maxHealth
            });
            
            AddComponent(entity, new PostTransformMatrix
            {
                Value = float4x4.Scale(1, 1, 1)
            });
        }
    }
}

public struct UnitDegradation : IComponentData
{
    public bool IsDegrading;
    public float Health;
    public float MaxHealth;
}

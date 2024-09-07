using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class MoveToAuthoring : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;

    public class Baker : Baker<MoveToAuthoring>
    {
        public override void Bake(MoveToAuthoring authoring)
        {
            var entity = GetEntity(authoring);
            AddComponent(entity, new MoveTo
            {
                Move = false,
                Position = default,
                LastMoveDir = default,
                MoveSpeed = 40f,
            });
        }
    }
}

public struct MoveTo : IComponentData
{
    public bool Move;
    public float3 Position;
    public float3 LastMoveDir;
    public float MoveSpeed;
}
using Unity.Entities;

public partial struct HandleCubesSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // foreach (var (localTransform, rotateSpeed, movement)
        //          in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotateSpeed>, RefRO<Movement>> ()
        //              .WithAll<RotatingCube>())
        // localTransform.ValueRW = localTransform.ValueRO.RotateY(rotateSpeed.ValueRO.Value * SystemAPI.Time.DeltaTime);
        // localTransform.ValueRW = localTransform.ValueRO.Translate(movement.ValueRO.MovementVector * SystemAPI.Time.DeltaTime);
        foreach (var rotatingMovingCubeAspect
                 in SystemAPI.Query<RotatingMovingCubeAspect>())
        {
            rotatingMovingCubeAspect.MoveAndRotate(SystemAPI.Time.DeltaTime);
        }
    }
}
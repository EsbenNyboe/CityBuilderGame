using Unity.Entities;

public struct MoodRestlessness : IComponentData
{
    public float Restlessness;
}

[UpdateInGroup(typeof(UnitBehaviourSystemGroup))]
public partial struct MoodRestlessnessSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (moodRestlessness, pathFollow) in SystemAPI.Query<RefRW<MoodRestlessness>, RefRO<PathFollow>>()
                     .WithAll<IsIdle>())
        {
            if (pathFollow.ValueRO.IsMoving())
            {
                continue;
            }

            moodRestlessness.ValueRW.Restlessness += SystemAPI.Time.DeltaTime;
        }
    }
}
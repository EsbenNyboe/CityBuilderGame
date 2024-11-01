using Unity.Entities;

[UpdateAfter(typeof(PathfindingSystem))]
public partial struct MoodRestlessnessSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (moodRestlessness, pathFollow) in SystemAPI.Query<RefRW<MoodRestlessness>, RefRO<PathFollow>>().WithAll<IsIdle>())
        {
            if (pathFollow.ValueRO.IsMoving())
            {
                continue;
            }

            moodRestlessness.ValueRW.TimeSpentDoingNothing += SystemAPI.Time.DeltaTime;
        }
    }
}
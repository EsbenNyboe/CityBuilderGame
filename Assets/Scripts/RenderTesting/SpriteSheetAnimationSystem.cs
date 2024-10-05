using Unity.Entities;

public partial class SpriteSheetAnimationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var spriteSheetAnimationData in SystemAPI.Query<RefRW<SpriteSheetAnimationData>>())
        {
            spriteSheetAnimationData.ValueRW.FrameTimer += SystemAPI.Time.DeltaTime;
            while (spriteSheetAnimationData.ValueRO.FrameTimer > spriteSheetAnimationData.ValueRO.FrameTimerMax)
            {
                spriteSheetAnimationData.ValueRW.FrameTimer -= spriteSheetAnimationData.ValueRO.FrameTimerMax;
                spriteSheetAnimationData.ValueRW.CurrentFrame =
                    (spriteSheetAnimationData.ValueRO.CurrentFrame + 1) % spriteSheetAnimationData.ValueRO.FrameCount;
            }
        }
    }
}
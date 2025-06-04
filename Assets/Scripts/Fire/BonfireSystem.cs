using CustomTimeCore;
using GridEntityNS;
using Rendering;
using Unity.Entities;

namespace Fire
{
    public partial struct BonfireSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CustomTime>();
            state.RequireForUpdate<WorldSpriteSheetManager>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var worldSpriteSheetManager = SystemAPI.GetSingleton<WorldSpriteSheetManager>();
            var bonfireBurningFrameCount = worldSpriteSheetManager.GetAnimationLength(WorldSpriteSheetEntryType.BonfireBurning);
            var bonfireBurningFrameInterval = worldSpriteSheetManager.GetFrameInterval(WorldSpriteSheetEntryType.BonfireBurning);
            var bonfireReadyFrameCount = worldSpriteSheetManager.GetAnimationLength(WorldSpriteSheetEntryType.BonfireReady);
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;

            foreach (var (bonfire, worldSpriteSheetState) in
                     SystemAPI.Query<RefRW<Bonfire>, RefRW<WorldSpriteSheetState>>().WithNone<Constructable>())
            {
                var isBurning = bonfire.ValueRO.IsBurning;
                var burnTimeLeft = bonfire.ValueRO.BurnTimeLeft;

                // TODO: Make villager ignite the fire
                isBurning = true;

                if (isBurning)
                {
                    burnTimeLeft -= SystemAPI.Time.DeltaTime * timeScale;
                }

                if (burnTimeLeft <= 0)
                {
                    isBurning = false;
                }

                var bonfireBurningFrame = (int)(burnTimeLeft / bonfireBurningFrameInterval % bonfireBurningFrameCount);
                worldSpriteSheetState.ValueRW.Uv = isBurning
                    ? worldSpriteSheetManager.GetUv(WorldSpriteSheetEntryType.BonfireBurning, bonfireBurningFrame)
                    : burnTimeLeft <= 0
                        ? worldSpriteSheetManager.GetUv(WorldSpriteSheetEntryType.BonfireDead)
                        : worldSpriteSheetManager.GetUv(WorldSpriteSheetEntryType.BonfireReady, bonfireReadyFrameCount - 1);

                bonfire.ValueRW.IsBurning = isBurning;
                bonfire.ValueRW.BurnTimeLeft = burnTimeLeft;
            }
        }
    }
}
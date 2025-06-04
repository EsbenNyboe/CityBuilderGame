using CustomTimeCore;
using GridEntityNS;
using Rendering;
using UnitBehaviours.AutonomousHarvesting.Model;
using Unity.Entities;

namespace Fire
{
    public struct BonfireManager : IComponentData
    {
        public float BurnTimePerLog;
    }

    public partial struct BonfireSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BonfireManager>();
            state.RequireForUpdate<CustomTime>();
            state.RequireForUpdate<WorldSpriteSheetManager>();
            state.EntityManager.CreateSingleton<BonfireManager>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var bonfireManager = SystemAPI.GetSingleton<BonfireManager>();
            if (bonfireManager.BurnTimePerLog <= 0)
            {
                foreach (var bonfire in SystemAPI.Query<RefRO<Bonfire>>())
                {
                    // HACK
                    bonfireManager.BurnTimePerLog = bonfire.ValueRO.BurnTimeLeft;
                }

                SystemAPI.SetSingleton(bonfireManager);
            }

            var worldSpriteSheetManager = SystemAPI.GetSingleton<WorldSpriteSheetManager>();
            var bonfireBurningFrameCount = worldSpriteSheetManager.GetAnimationLength(WorldSpriteSheetEntryType.BonfireBurning);
            var bonfireBurningFrameInterval = worldSpriteSheetManager.GetFrameInterval(WorldSpriteSheetEntryType.BonfireBurning);
            var bonfireReadyFrameCount = worldSpriteSheetManager.GetAnimationLength(WorldSpriteSheetEntryType.BonfireReady);
            var timeScale = SystemAPI.GetSingleton<CustomTime>().TimeScale;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (bonfire, worldSpriteSheetState, entity) in
                     SystemAPI.Query<RefRW<Bonfire>, RefRW<WorldSpriteSheetState>>().WithNone<Constructable>().WithEntityAccess())
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

                if (burnTimeLeft <= 0)
                {
                    ecb.AddComponent(entity, new Constructable
                    {
                        MaterialsRequired = 1, // HACK
                        Materials = 0
                    });
                    ecb.RemoveComponent<Renderable>(entity); // HACK
                    burnTimeLeft = bonfireManager.BurnTimePerLog;
                }

                bonfire.ValueRW.IsBurning = isBurning;
                bonfire.ValueRW.BurnTimeLeft = burnTimeLeft;
            }
        }
    }
}
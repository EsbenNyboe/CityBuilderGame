using GridEntityNS;
using Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace UnitBehaviours.AutonomousHarvesting
{
    [UpdateAfter(typeof(DamageableSystem))]
    public partial struct BerryBushSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldSpriteSheetManager>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var worldSpriteSheetManager = SystemAPI.GetSingleton<WorldSpriteSheetManager>();

            new SetBerryBushStateJob
            {
                WorldSpriteSheetManager = worldSpriteSheetManager
            }.ScheduleParallel(state.Dependency).Complete();
        }

        [BurstCompile]
        private partial struct SetBerryBushStateJob : IJobEntity
        {
            [ReadOnly] [NativeDisableContainerSafetyRestriction]
            public WorldSpriteSheetManager WorldSpriteSheetManager;

            public void Execute(in BerryBush _, in Damageable damageable, ref WorldSpriteSheetState worldSpriteSheetState)
            {
                var health = damageable.HealthNormalized;
                var damagedBerryBushVariants = WorldSpriteSheetManager.Entries[(int)WorldSpriteSheetEntryType.HarvestableBerryBushDamaged];
                var damagedBerryBushVariantsCount = damagedBerryBushVariants.EntryColumns.Length;

                if (TryGetDamageStateOfBerryBush(damagedBerryBushVariantsCount, health, out var frame))
                {
                    worldSpriteSheetState.Uv = WorldSpriteSheetManager.GetUv(WorldSpriteSheetEntryType.HarvestableBerryBushDamaged, frame);
                }
            }

            private static bool TryGetDamageStateOfBerryBush(int damagedBerryBushVariantsCount, float health, out int frame)
            {
                for (var i = 0; i < damagedBerryBushVariantsCount; i++)
                {
                    if (health < (float)(i + 1) / damagedBerryBushVariantsCount)
                    {
                        frame = i;
                        return true;
                    }
                }

                frame = -1;
                return false;
            }
        }
    }
}
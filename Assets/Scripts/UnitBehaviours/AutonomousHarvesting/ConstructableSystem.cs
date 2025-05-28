using GridEntityNS;
using Rendering;
using UnitBehaviours.AutonomousHarvesting.Model;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace UnitBehaviours.AutonomousHarvesting
{
    // [UpdateAfter(typeof(DamageableSystem))]
    public partial struct ConstructableSystem : ISystem
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

            new SetConstructableStateJob
            {
                WorldSpriteSheetManager = worldSpriteSheetManager
            }.ScheduleParallel(state.Dependency).Complete();
        }

        [BurstCompile]
        private partial struct SetConstructableStateJob : IJobEntity
        {
            [ReadOnly] [NativeDisableContainerSafetyRestriction]
            public WorldSpriteSheetManager WorldSpriteSheetManager;

            public void Execute(in Constructable constructable, in Renderable renderable, ref WorldSpriteSheetState worldSpriteSheetState)
            {
                var spriteSheetEntry = WorldSpriteSheetManager.Entries[(int)renderable.EntryType];
                var spriteSheetEntryLength = spriteSheetEntry.EntryColumns.Length;
                var materialsRequired = constructable.MaterialsRequired;
                var materials = constructable.Materials;
                var progress = materials / (float)materialsRequired;

                if (TryGetDamageStateOfConstructable(spriteSheetEntryLength, progress, out var frame))
                {
                    worldSpriteSheetState.Uv = WorldSpriteSheetManager.GetUv(renderable.EntryType, frame);
                }
            }

            private static bool TryGetDamageStateOfConstructable(int entryLength, float progress, out int frame)
            {
                for (var i = 0; i < entryLength; i++)
                {
                    if (progress <= (float)(i + 1) / entryLength)
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
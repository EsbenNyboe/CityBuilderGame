using Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;

namespace UnitBehaviours.AutonomousHarvesting
{
    public partial struct TreeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldSpriteSheetManager>();
            state.RequireForUpdate<GridManager>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gridManager = SystemAPI.GetSingleton<GridManager>();
            var worldSpriteSheetManager = SystemAPI.GetSingleton<WorldSpriteSheetManager>();

            new SetTreeStateJob
            {
                GridManager = gridManager,
                WorldSpriteSheetManager = worldSpriteSheetManager
            }.ScheduleParallel(state.Dependency).Complete();
        }

        [BurstCompile]
        private partial struct SetTreeStateJob : IJobEntity
        {
            [ReadOnly] public GridManager GridManager;

            [ReadOnly] [NativeDisableContainerSafetyRestriction]
            public WorldSpriteSheetManager WorldSpriteSheetManager;

            public void Execute(in Tree _, in LocalTransform localTransform, ref WorldSpriteSheetState worldSpriteSheetState)
            {
                var gridIndex = GridManager.GetIndex(localTransform.Position);
                var health = GridManager.GetHealthNormalized(gridIndex);
                var damagedTreeVariants = WorldSpriteSheetManager.Entries[(int)WorldSpriteSheetEntryType.TreeDamaged];
                var damagedTreeVariantsCount = damagedTreeVariants.EntryColumns.Length;

                if (TryGetDamageStateOfTree(damagedTreeVariantsCount, health, out var frame))
                {
                    worldSpriteSheetState.Uv = WorldSpriteSheetManager.GetUv(WorldSpriteSheetEntryType.TreeDamaged, frame);
                }
            }

            private static bool TryGetDamageStateOfTree(int damagedTreeVariantsCount, float health, out int frame)
            {
                for (var i = 0; i < damagedTreeVariantsCount; i++)
                {
                    if (health < (float)(i + 1) / damagedTreeVariantsCount)
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
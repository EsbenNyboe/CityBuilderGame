using System;
using UnityEngine;

namespace Rendering
{
    /// <summary>
    ///     Entries are ordered by the sprite-sheet coordinates, they belong to, UNLIKE the entry-ordering of <see cref="WorldSpriteSheetManager" />
    /// </summary>
    public class WorldSpriteSheetConfig : MonoBehaviour
    {
        public static WorldSpriteSheetConfig Instance;
        public Mesh UnitMesh;
        public Material UnitMaterial;

        public SpriteSheetEntry[] SpriteSheetEntries;

        public int ColumnCount;
        public int RowCount;

        [HideInInspector] public bool IsDirty = true;

        private void Awake()
        {
            Instance = this;
        }

        private void OnValidate()
        {
            IsDirty = true;
        }
    }

    [Serializable]
    public struct SpriteSheetEntry
    {
        public WorldSpriteSheetEntryType Identifier;
        [Min(1)] public int FrameCount;
        [Min(0.001f)] public float FrameInterval;
    }

    public enum WorldSpriteSheetEntryType
    {
        None,
        Idle,
        Walk,
        IdleHolding,
        WalkHolding,
        Talk,
        Sleep,
        ItemWood,
        ItemMeat,
        ItemBerries,
        BuildingStorageA,
        BuildingStorageB,
        BoarStand,
        BoarRun,
        SpearHolding,
        SpearThrowing,
        Spear,
        DropPoint,
        TreeDamaged,
        Tree,
        Bed
    }
}
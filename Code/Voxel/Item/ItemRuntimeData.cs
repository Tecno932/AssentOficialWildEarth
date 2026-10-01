using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct ItemRuntimeData
    {
        public ushort Id;

        public ItemType ItemType;

        public ushort BlockId;

        public ushort MaxStackSize;

        public ToolType ToolType;

        public byte ToolLevel;

        public float ToolSpeedMultiplier;

        public bool HasDurability;

        public ushort MaxDurability;

        public AtlasTileCoordinate Icon;

        public bool RepresentsBlock =>
            ItemType == ItemType.Block &&
            BlockId != BlockIds.Air;

        public bool IsTool =>
            ItemType == ItemType.Tool;

        public bool IsStackable =>
            MaxStackSize > 1;

        public bool UsesToolType(ToolType type) =>
            IsTool &&
            ToolType == type;
    }
}
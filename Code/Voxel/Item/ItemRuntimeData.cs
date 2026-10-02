using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct ItemRuntimeData
    {
        public ushort Id;

        public ItemType ItemType;

        public ushort BlockId;

        public ToolType ToolType;

        public byte ToolLevel;

        public float ToolSpeed;

        public float Damage;

        public float Sharpness;

        public WeaponType WeaponType;

        public ushort MaxDurability;

        public ushort MaxStackSize;

        public AtlasTileCoordinate Icon;

        public bool RepresentsBlock =>
            ItemType == ItemType.Block &&
            BlockId != BlockIds.Air;

        public bool IsTool =>
            ItemType == ItemType.Tool;

        public bool IsWeapon =>
            ItemType == ItemType.Weapon;

        public bool HasDurability =>
            MaxDurability > 0;

        public bool IsStackable =>
            MaxStackSize > 1;

        public bool UsesToolType(ToolType type) =>
            IsTool &&
            ToolType == type;

        public bool UsesWeaponType(WeaponType type) =>
            IsWeapon &&
            WeaponType == type;
    }
}
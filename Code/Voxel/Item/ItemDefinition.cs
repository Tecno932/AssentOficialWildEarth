using System;
using UnityEngine;

namespace WildEarth.Voxel
{
    public enum ItemType : byte
    {
        None = 0,
        Block = 1,
        Tool = 2,
        Weapon = 3,
        Armor = 4,
        Consumable = 5,
        Material = 6,
        Miscellaneous = 7
    }

    [CreateAssetMenu(
        fileName = "ItemDefinition",
        menuName = "WildEarth/Voxel/Item Definition"
    )]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private ushort id;

        [SerializeField]
        private string itemName;

        [SerializeField]
        private ItemType itemType =
            ItemType.Miscellaneous;

        [Header("Block")]
        [SerializeField]
        private ushort blockId;

        [Header("Stacking")]
        [SerializeField]
        [Min(1)]
        private ushort maxStackSize = 64;

        [Header("Tool")]
        [SerializeField]
        private ToolType toolType =
            ToolType.None;

        [SerializeField]
        [Min(0)]
        private byte toolLevel;

        [SerializeField]
        [Min(0f)]
        private float toolSpeedMultiplier = 1f;

        [Header("Durability")]
        [SerializeField]
        private bool hasDurability;

        [SerializeField]
        [Min(0)]
        private ushort maxDurability;

        [Header("Visual")]
        [SerializeField]
        private AtlasTileCoordinate icon;

        public ushort Id => id;

        public string ItemName => itemName;

        public ItemType ItemType => itemType;

        public ushort BlockId => blockId;

        public bool RepresentsBlock =>
            itemType == ItemType.Block &&
            blockId != BlockIds.Air;

        public ushort MaxStackSize => maxStackSize;

        public ToolType ToolType => toolType;

        public byte ToolLevel => toolLevel;

        public float ToolSpeedMultiplier =>
            toolSpeedMultiplier;

        public bool HasDurability =>
            hasDurability;

        public ushort MaxDurability =>
            maxDurability;

        public AtlasTileCoordinate Icon =>
            icon;

#if UNITY_EDITOR

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                itemName = name;
            }

            if (maxStackSize < 1)
            {
                maxStackSize = 1;
            }

            if (toolSpeedMultiplier < 0f)
            {
                toolSpeedMultiplier = 0f;
            }

            if (hasDurability &&
                maxDurability == 0)
            {
                maxDurability = 1;
            }

            if (icon.Column < 0)
            {
                icon.Column = 0;
            }

            if (icon.Row < 0)
            {
                icon.Row = 0;
            }

            if (itemType != ItemType.Block)
            {
                blockId = BlockIds.Air;
            }

            if (itemType != ItemType.Tool)
            {
                toolType = ToolType.None;
                toolLevel = 0;
                toolSpeedMultiplier = 1f;
                hasDurability = false;
                maxDurability = 0;
            }
        }

#endif
    }
}
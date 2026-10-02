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

        [Header("Tool")]
        [SerializeField]
        private ToolType toolType =
            ToolType.None;

        [SerializeField]
        [Min(0)]
        private byte toolLevel;

        [SerializeField]
        [Min(0f)]
        private float toolSpeed = 1f;

        [SerializeField]
        [Min(0f)]
        private float damage = 1f;

        [SerializeField]
        [Range(0f, 100f)]
        private float sharpness = 100f;

        [Header("Weapon")]
        [SerializeField]
        private WeaponType weaponType =
            WeaponType.None;

        [Header("Durability")]
        [SerializeField]
        private ushort maxDurability;

        [Header("Stacking")]
        [SerializeField]
        private ushort maxStackSize = 64;

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

        public ToolType ToolType => toolType;

        public byte ToolLevel => toolLevel;

        public float ToolSpeed => toolSpeed;

        public float Damage => damage;

        public float Sharpness => sharpness;

        public WeaponType WeaponType => weaponType;

        public ushort MaxDurability => maxDurability;

        public bool HasDurability =>
            maxDurability > 0;

        public ushort MaxStackSize => maxStackSize;

        public AtlasTileCoordinate Icon => icon;

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

            if (toolSpeed < 0f)
            {
                toolSpeed = 0f;
            }

            if (damage < 0f)
            {
                damage = 0f;
            }

            sharpness =
                Mathf.Clamp(
                    sharpness,
                    0f,
                    100f
                );

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
                toolSpeed = 0f;
            }

            if (itemType != ItemType.Weapon)
            {
                weaponType = WeaponType.None;
            }

            if (itemType != ItemType.Tool &&
                itemType != ItemType.Weapon)
            {
                damage = 0f;
                sharpness = 0f;
                maxDurability = 0;
            }

            if (itemType == ItemType.Tool &&
                toolType == ToolType.None)
            {
                toolSpeed = 0f;
            }
        }

#endif
    }
}
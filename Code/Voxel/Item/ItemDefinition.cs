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
        [SerializeField] private ushort id;
        [SerializeField] private string itemName;
        [SerializeField] private ItemType itemType =
            ItemType.Miscellaneous;

        [Header("References")]
        [SerializeField] private BlockDefinition blockDefinition;
        [SerializeField] private ToolDefinition toolDefinition;
        [SerializeField] private MaterialDefinition materialDefinition;
        [SerializeField] private AlloyDefinition alloyDefinition;

        [Header("Legacy Tool Data")]
        [SerializeField] private ToolType toolType = ToolType.None;
        [SerializeField, Min(0)] private byte toolLevel;
        [SerializeField, Min(0f)] private float toolSpeed = 1f;

        [Header("Legacy Weapon Data")]
        [SerializeField, Min(0f)] private float damage = 1f;
        [SerializeField, Range(0f, 100f)] private float sharpness = 100f;
        [SerializeField] private WeaponType weaponType = WeaponType.None;

        [Header("Durability")]
        [SerializeField] private ushort maxDurability;

        [Header("Stacking")]
        [SerializeField] private ushort maxStackSize = 64;

        [Header("Visual")]
        [SerializeField] private AtlasTileCoordinate icon;

        public ushort Id => id;
        public string ItemName => itemName;
        public ItemType ItemType => itemType;

        public BlockDefinition BlockDefinition => blockDefinition;
        public ToolDefinition ToolDefinition => toolDefinition;
        public MaterialDefinition MaterialDefinition => materialDefinition;
        public AlloyDefinition AlloyDefinition => alloyDefinition;

        public ushort BlockId =>
            blockDefinition != null
                ? blockDefinition.Id
                : BlockIds.Air;

        public bool RepresentsBlock =>
            itemType == ItemType.Block &&
            blockDefinition != null &&
            BlockId != BlockIds.Air;

        public ToolType ToolType =>
            toolDefinition != null
                ? toolDefinition.ToolType
                : toolType;

        public byte ToolLevel => toolLevel;
        public float ToolSpeed => toolSpeed;
        public float Damage => damage;
        public float Sharpness => sharpness;
        public WeaponType WeaponType => weaponType;

        public ushort MaxDurability => maxDurability;
        public bool HasDurability => maxDurability > 0;
        public ushort MaxStackSize => maxStackSize;
        public AtlasTileCoordinate Icon => icon;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemName))
                itemName = name;

            maxStackSize = (ushort)Mathf.Max(1, maxStackSize);

            if (icon.Column < 0)
                icon.Column = 0;

            if (icon.Row < 0)
                icon.Row = 0;

            switch (itemType)
            {
                case ItemType.Block:
                    toolDefinition = null;
                    materialDefinition = null;
                    alloyDefinition = null;
                    toolType = ToolType.None;
                    toolLevel = 0;
                    toolSpeed = 0f;
                    damage = 0f;
                    sharpness = 0f;
                    weaponType = WeaponType.None;
                    break;

                case ItemType.Tool:
                    blockDefinition = null;
                    materialDefinition = null;
                    alloyDefinition = null;
                    weaponType = WeaponType.None;
                    damage = 0f;
                    sharpness = 0f;

                    if (toolDefinition != null)
                        toolType = toolDefinition.ToolType;
                    break;

                case ItemType.Material:
                    blockDefinition = null;
                    toolDefinition = null;
                    weaponType = WeaponType.None;
                    toolType = ToolType.None;
                    toolLevel = 0;
                    toolSpeed = 0f;
                    damage = 0f;
                    sharpness = 0f;

                    if (alloyDefinition != null)
                        materialDefinition = null;
                    break;

                case ItemType.Weapon:
                    blockDefinition = null;
                    toolDefinition = null;
                    materialDefinition = null;
                    alloyDefinition = null;
                    toolType = ToolType.None;
                    toolLevel = 0;
                    toolSpeed = 0f;
                    break;

                default:
                    blockDefinition = null;
                    toolDefinition = null;
                    materialDefinition = null;
                    alloyDefinition = null;
                    toolType = ToolType.None;
                    toolLevel = 0;
                    toolSpeed = 0f;
                    weaponType = WeaponType.None;
                    damage = 0f;
                    sharpness = 0f;
                    maxDurability = 0;
                    break;
            }

            if (itemType == ItemType.Material &&
                materialDefinition == null &&
                alloyDefinition == null)
            {
                // Se permite dejar la referencia vacía
                // mientras se configura el asset.
            }
        }
    }
}

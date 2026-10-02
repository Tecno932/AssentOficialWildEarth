using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct ItemStack
    {
        public ushort ItemId;
        public ushort Quantity;
        public ushort Durability;

        public bool IsEmpty =>
            ItemId == 0 ||
            Quantity == 0;

        public ItemStack(
            ushort itemId,
            ushort quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
            Durability = 0;
        }

        public ItemStack(
            ushort itemId,
            ushort quantity,
            ushort durability)
        {
            ItemId = itemId;
            Quantity = quantity;
            Durability = durability;
        }

        public void Clear()
        {
            ItemId = 0;
            Quantity = 0;
            Durability = 0;
        }
    }
}
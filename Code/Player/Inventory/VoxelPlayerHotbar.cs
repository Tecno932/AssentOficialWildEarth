using UnityEngine;
using UnityEngine.InputSystem;

namespace WildEarth.Voxel
{
    public enum VoxelHotbarSlotType : byte
    {
        Empty = 0,
        Item = 1
    }

    public sealed class VoxelPlayerHotbar : MonoBehaviour
    {
        public const int SlotCount = 9;

        [Header("References")]
        [SerializeField]
        private ItemRegistry itemRegistry;

        [Header("Initial Item Slots")]
        [SerializeField]
        private ushort[] initialItemIds =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        [SerializeField]
        private ushort initialItemQuantity = 64;

        private readonly ItemStack[] itemSlots =
            new ItemStack[SlotCount];

        private readonly ushort[] lastInitialItemIds =
            new ushort[SlotCount];

        private int selectedSlot;
        private int configurationVersion;

        public int SelectedSlot =>
            selectedSlot;

        public VoxelHotbarSlotType SelectedSlotType =>
            itemSlots[selectedSlot].IsEmpty
                ? VoxelHotbarSlotType.Empty
                : VoxelHotbarSlotType.Item;

        public ushort SelectedItemId =>
            itemSlots[selectedSlot].ItemId;

        public ushort SelectedItemQuantity =>
            itemSlots[selectedSlot].Quantity;

        public ushort SelectedItemDurability =>
            itemSlots[selectedSlot].Durability;

        public int ConfigurationVersion =>
            configurationVersion;

        public VoxelHotbarSlotType GetSlotType(
            int slot)
        {
            if (slot < 0 ||
                slot >= SlotCount)
            {
                return VoxelHotbarSlotType.Empty;
            }

            return itemSlots[slot].IsEmpty
                ? VoxelHotbarSlotType.Empty
                : VoxelHotbarSlotType.Item;
        }

        public ushort GetItemId(
            int slot)
        {
            if (slot < 0 ||
                slot >= SlotCount)
            {
                return 0;
            }

            return itemSlots[slot].ItemId;
        }

        public ushort GetItemQuantity(
            int slot)
        {
            if (slot < 0 ||
                slot >= SlotCount)
            {
                return 0;
            }

            return itemSlots[slot].Quantity;
        }

        public ushort GetItemDurability(
            int slot)
        {
            if (slot < 0 ||
                slot >= SlotCount)
            {
                return 0;
            }

            return itemSlots[slot].Durability;
        }

        public ItemStack GetItemStack(
            int slot)
        {
            if (slot < 0 ||
                slot >= SlotCount)
            {
                return default;
            }

            return itemSlots[slot];
        }

        public bool TryGetSelectedItem(
            out ItemDefinition definition)
        {
            definition = null;

            ushort itemId =
                SelectedItemId;

            if (itemId == 0 ||
                itemRegistry == null)
            {
                return false;
            }

            return itemRegistry.TryGetDefinition(
                itemId,
                out definition
            );
        }

        public bool TryGetSelectedRuntimeItem(
            out ItemRuntimeData data)
        {
            data = default;

            ushort itemId =
                SelectedItemId;

            if (itemId == 0 ||
                SelectedItemQuantity == 0 ||
                itemRegistry == null)
            {
                return false;
            }

            return itemRegistry.TryGetRuntimeData(
                itemId,
                out data
            );
        }

        public bool TryGetSelectedBlockId(
            out ushort blockId)
        {
            blockId = BlockIds.Air;

            if (!TryGetSelectedRuntimeItem(
                    out ItemRuntimeData data))
            {
                return false;
            }

            if (!data.RepresentsBlock)
            {
                return false;
            }

            blockId = data.BlockId;

            return true;
        }

        private void Awake()
        {
            InitializeSlots();
        }

        private void Update()
        {
            SyncInitialSlots();

            HandleNumberKeys();
            HandleMouseWheel();
        }

        private void InitializeSlots()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                ushort itemId =
                    GetInitialItemId(i);

                itemId =
                    ValidateItemId(
                        itemId,
                        i
                    );

                ushort quantity =
                    itemId == 0
                        ? (ushort)0
                        : GetInitialQuantity(
                            itemId
                        );

                itemSlots[i] =
                    CreateItemStack(
                        itemId,
                        quantity
                    );

                lastInitialItemIds[i] =
                    GetInitialItemId(i);
            }

            selectedSlot = 0;
            configurationVersion++;
        }

        private void SyncInitialSlots()
        {
            bool changed = false;

            for (int i = 0; i < SlotCount; i++)
            {
                ushort itemId =
                    GetInitialItemId(i);

                if (itemId ==
                    lastInitialItemIds[i])
                {
                    continue;
                }

                lastInitialItemIds[i] =
                    itemId;

                itemId =
                    ValidateItemId(
                        itemId,
                        i
                    );

                ushort quantity =
                    itemId == 0
                        ? (ushort)0
                        : GetInitialQuantity(
                            itemId
                        );

                itemSlots[i] =
                    CreateItemStack(
                        itemId,
                        quantity
                    );

                changed = true;
            }

            if (changed)
            {
                configurationVersion++;
            }
        }

        private ushort GetInitialItemId(
            int index)
        {
            if (initialItemIds == null)
            {
                return 0;
            }

            if (index < 0 ||
                index >= initialItemIds.Length)
            {
                return 0;
            }

            return initialItemIds[index];
        }

        private ushort GetInitialQuantity(
            ushort itemId)
        {
            if (itemId == 0 ||
                itemRegistry == null)
            {
                return 0;
            }

            if (!itemRegistry.TryGetRuntimeData(
                    itemId,
                    out ItemRuntimeData data))
            {
                return 0;
            }

            if (data.HasDurability)
            {
                return 1;
            }

            ushort maxStack =
                data.MaxStackSize;

            if (maxStack == 0)
            {
                return 0;
            }

            return (ushort)Mathf.Min(
                initialItemQuantity,
                maxStack
            );
        }

        private ItemStack CreateItemStack(
            ushort itemId,
            ushort quantity)
        {
            if (itemId == 0 ||
                quantity == 0 ||
                itemRegistry == null)
            {
                return default;
            }

            if (!itemRegistry.TryGetRuntimeData(
                    itemId,
                    out ItemRuntimeData data))
            {
                return default;
            }

            if (data.HasDurability)
            {
                return new ItemStack(
                    itemId,
                    1,
                    data.MaxDurability
                );
            }

            return new ItemStack(
                itemId,
                quantity
            );
        }

        private void HandleNumberKeys()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                SelectSlot(0);
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                SelectSlot(1);
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                SelectSlot(2);
            }
            else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                SelectSlot(3);
            }
            else if (Keyboard.current.digit5Key.wasPressedThisFrame)
            {
                SelectSlot(4);
            }
            else if (Keyboard.current.digit6Key.wasPressedThisFrame)
            {
                SelectSlot(5);
            }
            else if (Keyboard.current.digit7Key.wasPressedThisFrame)
            {
                SelectSlot(6);
            }
            else if (Keyboard.current.digit8Key.wasPressedThisFrame)
            {
                SelectSlot(7);
            }
            else if (Keyboard.current.digit9Key.wasPressedThisFrame)
            {
                SelectSlot(8);
            }
        }

        private void HandleMouseWheel()
        {
            if (Mouse.current == null)
            {
                return;
            }

            float scroll =
                Mouse.current.scroll.ReadValue().y;

            if (scroll > 0f)
            {
                SelectPreviousSlot();
            }
            else if (scroll < 0f)
            {
                SelectNextSlot();
            }
        }

        private void SelectPreviousSlot()
        {
            selectedSlot--;

            if (selectedSlot < 0)
            {
                selectedSlot =
                    SlotCount - 1;
            }
        }

        private void SelectNextSlot()
        {
            selectedSlot++;

            if (selectedSlot >= SlotCount)
            {
                selectedSlot = 0;
            }
        }

        public void SelectSlot(
            int slot)
        {
            if (slot < 0 ||
                slot >= SlotCount)
            {
                return;
            }

            selectedSlot = slot;
        }

        public void SetItemSlot(
            int slot,
            ushort itemId)
        {
            if (slot < 0 ||
                slot >= SlotCount)
            {
                return;
            }

            if (itemId != 0 &&
                !IsValidItem(itemId))
            {
                Debug.LogWarning(
                    $"VoxelPlayerHotbar: " +
                    $"no se puede colocar el ítem " +
                    $"{itemId} porque no existe " +
                    $"en ItemRegistry.",
                    this
                );

                return;
            }

            ushort quantity =
                itemId == 0
                    ? (ushort)0
                    : GetInitialQuantity(
                        itemId
                    );

            itemSlots[slot] =
                CreateItemStack(
                    itemId,
                    quantity
                );

            SetInitialItemId(
                slot,
                itemId
            );

            configurationVersion++;
        }

        public bool TryAddItemForBlock(
            ushort blockId,
            ushort quantity)
        {
            if (itemRegistry == null ||
                blockId == BlockIds.Air ||
                quantity == 0)
            {
                return false;
            }

            if (!itemRegistry.TryGetItemIdForBlock(
                    blockId,
                    out ushort itemId))
            {
                Debug.LogWarning(
                    $"VoxelPlayerHotbar: no existe un " +
                    $"ItemDefinition para BlockID {blockId}.",
                    this
                );

                return false;
            }

            return TryAddItem(
                itemId,
                quantity
            );
        }

        public bool TryAddItem(
            ushort itemId,
            ushort quantity)
        {
            if (itemId == 0 ||
                quantity == 0 ||
                itemRegistry == null)
            {
                return false;
            }

            if (!itemRegistry.TryGetRuntimeData(
                    itemId,
                    out ItemRuntimeData data))
            {
                return false;
            }

            if (data.HasDurability)
            {
                for (int i = 0;
                    i < SlotCount;
                    i++)
                {
                    if (!itemSlots[i].IsEmpty)
                    {
                        continue;
                    }

                    itemSlots[i] =
                        CreateItemStack(
                            itemId,
                            1
                        );

                    configurationVersion++;

                    return quantity == 1;
                }

                return false;
            }

            ushort remaining =
                quantity;

            for (int i = 0;
                i < SlotCount && remaining > 0;
                i++)
            {
                if (itemSlots[i].ItemId != itemId)
                {
                    continue;
                }

                ushort maxStack =
                    data.MaxStackSize;

                if (itemSlots[i].Quantity >= maxStack)
                {
                    continue;
                }

                ushort available =
                    (ushort)(
                        maxStack -
                        itemSlots[i].Quantity
                    );

                ushort amount =
                    remaining < available
                        ? remaining
                        : available;

                itemSlots[i].Quantity +=
                    amount;

                remaining -=
                    amount;
            }

            for (int i = 0;
                i < SlotCount && remaining > 0;
                i++)
            {
                if (!itemSlots[i].IsEmpty)
                {
                    continue;
                }

                ushort maxStack =
                    data.MaxStackSize;

                ushort amount =
                    remaining < maxStack
                        ? remaining
                        : maxStack;

                itemSlots[i] =
                    CreateItemStack(
                        itemId,
                        amount
                    );

                remaining -=
                    amount;
            }

            if (remaining != quantity)
            {
                configurationVersion++;
            }

            return remaining == 0;
        }

        public bool TryDamageSelectedItem(
            ushort amount = 1)
        {
            if (amount == 0)
            {
                return true;
            }

            ItemStack stack =
                itemSlots[selectedSlot];

            if (stack.IsEmpty ||
                itemRegistry == null)
            {
                return false;
            }

            if (!itemRegistry.TryGetRuntimeData(
                    stack.ItemId,
                    out ItemRuntimeData data))
            {
                return false;
            }

            if (!data.HasDurability)
            {
                return true;
            }

            if (stack.Durability == 0)
            {
                stack.Clear();

                itemSlots[selectedSlot] =
                    stack;

                configurationVersion++;

                return true;
            }

            if (amount >= stack.Durability)
            {
                stack.Clear();
            }
            else
            {
                stack.Durability -= amount;
            }

            itemSlots[selectedSlot] =
                stack;

            configurationVersion++;

            return true;
        }

        public bool TryConsumeSelectedItem(
            ushort quantity = 1)
        {
            if (quantity == 0)
            {
                return true;
            }

            ItemStack stack =
                itemSlots[selectedSlot];

            if (stack.IsEmpty ||
                stack.Quantity < quantity)
            {
                return false;
            }

            stack.Quantity -= quantity;

            if (stack.Quantity == 0)
            {
                stack.Clear();
            }

            itemSlots[selectedSlot] =
                stack;

            configurationVersion++;

            return true;
        }

        public void ClearSlot(
            int slot)
        {
            if (slot < 0 ||
                slot >= SlotCount)
            {
                return;
            }

            itemSlots[slot] =
                default;

            configurationVersion++;
        }

        private void SetInitialItemId(
            int index,
            ushort itemId)
        {
            if (initialItemIds == null ||
                initialItemIds.Length != SlotCount)
            {
                initialItemIds =
                    new ushort[SlotCount];
            }

            initialItemIds[index] =
                itemId;

            lastInitialItemIds[index] =
                itemId;
        }

        private ushort ValidateItemId(
            ushort itemId,
            int slot)
        {
            if (itemId == 0)
            {
                return 0;
            }

            if (IsValidItem(itemId))
            {
                return itemId;
            }

            Debug.LogWarning(
                $"VoxelPlayerHotbar: " +
                $"el ítem con ID {itemId} " +
                $"no existe en ItemRegistry. " +
                $"El slot {slot + 1} quedará vacío.",
                this
            );

            return 0;
        }

        private bool IsValidItem(
            ushort itemId)
        {
            if (itemRegistry == null)
            {
                return false;
            }

            return itemRegistry.TryGetDefinition(
                itemId,
                out _
            );
        }
    }
}
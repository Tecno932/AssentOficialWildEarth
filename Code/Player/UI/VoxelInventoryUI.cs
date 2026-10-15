using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace WildEarth.Voxel
{
    public sealed class VoxelInventoryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VoxelPlayerHotbar hotbar;
        [SerializeField] private VoxelHotbarUI hotbarUI;
        [SerializeField] private VoxelPlayerController playerController;
        [SerializeField] private ItemRegistry itemRegistry;
        [SerializeField] private VoxelAtlasSettings atlasSettings;
        [SerializeField] private Texture2D atlasTexture;

        [Header("Layout")]
        [SerializeField, Min(24f)] private float slotSize = 52f;
        [SerializeField, Min(0f)] private float slotSpacing = 5f;

        [Header("Mouse Interaction")]
        [SerializeField, Min(0.05f)] private float holdStartDelay = 0.25f;
        [SerializeField, Min(0.02f)] private float holdTakeInterval = 0.08f;

        private const int Columns = 9;
        private const int Rows = 4;

        private RectTransform panel;
        private Image[] slotBackgrounds;
        private RawImage[] slotIcons;
        private Text[] quantityTexts;
        private Image[] durabilityBackgrounds;
        private Image[] durabilityFills;

        private RectTransform carriedVisual;
        private RawImage carriedIcon;
        private Text carriedQuantityText;
        private Image carriedBarBackground;
        private Image carriedBarFill;

        private ItemStack carriedStack;
        private bool carriedStackSplit;
        private bool isOpen;
        private int activeHoldSlot = -1;
        private PointerEventData.InputButton activeHoldButton;
        private float holdElapsed;
        private float holdTakeTimer;
        private bool holdDidTake;
        private bool suppressNextClick;
        private int lastConfigurationVersion = -1;

        private void Awake()
        {
            if (hotbar == null ||
                playerController == null ||
                itemRegistry == null ||
                atlasSettings == null ||
                atlasTexture == null)
            {
                throw new System.InvalidOperationException(
                    "VoxelInventoryUI requiere Hotbar, " +
                    "PlayerController, ItemRegistry, " +
                    "VoxelAtlasSettings y atlasTexture."
                );
            }

            BuildUI();
            BuildCarriedVisual();

            panel.gameObject.SetActive(false);
            carriedVisual.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    SetOpen(!isOpen);
                }
                else if (isOpen &&
                         Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    SetOpen(false);
                }
            }

            if (!isOpen)
                return;

            UpdateHeldMouse();
            UpdateCarriedVisual();

            if (lastConfigurationVersion != hotbar.ConfigurationVersion)
                RefreshSlots();
        }

        private void BuildUI()
        {
            panel = CreateRect("InventoryPanel", transform);

            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);

            float width =
                Columns * slotSize +
                (Columns - 1) * slotSpacing + 32f;

            float height =
                Rows * slotSize +
                (Rows - 1) * slotSpacing + 76f;

            panel.sizeDelta = new Vector2(width, height);

            Image panelBackground = panel.gameObject.AddComponent<Image>();
            panelBackground.color = new Color(0.08f, 0.08f, 0.08f, 0.96f);

            CreateLabel(
                "INVENTARIO",
                panel,
                new Vector2(0f, -19f),
                20,
                TextAnchor.MiddleCenter
            );

            slotBackgrounds = new Image[36];
            slotIcons = new RawImage[36];
            quantityTexts = new Text[36];
            durabilityBackgrounds = new Image[36];
            durabilityFills = new Image[36];

            float startX = -width * 0.5f + 16f;
            float startY = height * 0.5f - 58f;

            for (int displayIndex = 0; displayIndex < 36; displayIndex++)
            {
                int slotIndex = displayIndex < 27
                    ? displayIndex + 9
                    : displayIndex - 27;

                int column = displayIndex % Columns;
                int row = displayIndex / Columns;

                RectTransform slot = CreateRect(
                    $"Slot_{slotIndex}",
                    panel
                );

                slot.anchorMin = new Vector2(0.5f, 0.5f);
                slot.anchorMax = new Vector2(0.5f, 0.5f);
                slot.pivot = new Vector2(0.5f, 0.5f);

                slot.anchoredPosition = new Vector2(
                    startX + column * (slotSize + slotSpacing)
                        + slotSize * 0.5f,
                    startY - row * (slotSize + slotSpacing)
                        - slotSize * 0.5f
                );

                slot.sizeDelta = new Vector2(slotSize, slotSize);

                Image background = slot.gameObject.AddComponent<Image>();
                background.color = new Color(0.20f, 0.20f, 0.20f, 1f);

                Button button = slot.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                button.transition = Selectable.Transition.None;

                VoxelInventorySlotInput input =
                    slot.gameObject.AddComponent<VoxelInventorySlotInput>();

                input.Initialize(this, slotIndex);

                slotBackgrounds[slotIndex] = background;
                slotIcons[slotIndex] = CreateIcon(slot);
                quantityTexts[slotIndex] = CreateQuantity(slot);

                CreateDurabilityBar(slot, slotIndex);
            }

            CreateLabel(
                "Inventario",
                panel,
                new Vector2(0f, 18f),
                12,
                TextAnchor.MiddleCenter
            );

            RefreshSlots();
        }

        private void BuildCarriedVisual()
        {
            carriedVisual = CreateRect("CarriedItem", transform);

            carriedVisual.anchorMin = new Vector2(0f, 1f);
            carriedVisual.anchorMax = new Vector2(0f, 1f);
            carriedVisual.pivot = new Vector2(0f, 1f);
            carriedVisual.sizeDelta = new Vector2(48f, 58f);

            carriedIcon = CreateIcon(carriedVisual);
            carriedIcon.rectTransform.offsetMin = new Vector2(4f, 14f);
            carriedIcon.rectTransform.offsetMax = new Vector2(-4f, -4f);

            carriedQuantityText = CreateLabel(
                "",
                carriedVisual,
                new Vector2(0f, -43f),
                13,
                TextAnchor.MiddleCenter
            );

            carriedQuantityText.rectTransform.anchorMin = new Vector2(0f, 0f);
            carriedQuantityText.rectTransform.anchorMax = new Vector2(1f, 0f);
            carriedQuantityText.rectTransform.pivot = new Vector2(0.5f, 0f);
            carriedQuantityText.rectTransform.anchoredPosition = new Vector2(0f, 13f);
            carriedQuantityText.rectTransform.sizeDelta = new Vector2(0f, 18f);

            GameObject barBackground = new GameObject(
                "CarriedQuantityBackground",
                typeof(RectTransform),
                typeof(Image)
            );

            barBackground.transform.SetParent(carriedVisual, false);

            RectTransform bgRect = barBackground.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(1f, 0f);
            bgRect.pivot = new Vector2(0.5f, 0f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(-2f, 5f);

            carriedBarBackground = barBackground.GetComponent<Image>();
            carriedBarBackground.color = new Color(0f, 0f, 0f, 0.9f);
            carriedBarBackground.raycastTarget = false;

            GameObject barFill = new GameObject(
                "CarriedQuantityFill",
                typeof(RectTransform),
                typeof(Image)
            );

            barFill.transform.SetParent(barBackground.transform, false);

            RectTransform fillRect = barFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = Vector2.zero;

            carriedBarFill = barFill.GetComponent<Image>();
            carriedBarFill.color = new Color(0.2f, 0.9f, 0.2f, 1f);
            carriedBarFill.raycastTarget = false;
        }

        public void BeginSlotPointer(
            int slot,
            PointerEventData.InputButton button)
        {
            if (!isOpen)
                return;

            activeHoldSlot = slot;
            activeHoldButton = button;
            holdElapsed = 0f;
            holdTakeTimer = 0f;
            holdDidTake = false;
            suppressNextClick = false;
        }

        public void EndSlotPointer(
            int slot,
            PointerEventData.InputButton button)
        {
            if (activeHoldSlot != slot || activeHoldButton != button)
                return;

            bool didHold = holdDidTake;

            activeHoldSlot = -1;

            if (didHold)
            {
                suppressNextClick = true;
                RefreshSlots();
                return;
            }

            HandleSlotClick(slot, button);
        }

        public void CancelSlotPointer(int slot)
        {
            if (activeHoldSlot == slot)
                activeHoldSlot = -1;
        }

        private void UpdateHeldMouse()
        {
            if (activeHoldSlot < 0 || Mouse.current == null)
                return;

            bool pressed = activeHoldButton == PointerEventData.InputButton.Left
                ? Mouse.current.leftButton.isPressed
                : Mouse.current.rightButton.isPressed;

            if (!pressed)
                return;

            // Solo el botón izquierdo recoge objetos progresivamente.
            if (activeHoldButton != PointerEventData.InputButton.Left ||
                !carriedStack.IsEmpty)
                return;

            ItemStack source = hotbar.GetStorageItemStack(activeHoldSlot);

            if (source.IsEmpty ||
                !itemRegistry.TryGetRuntimeData(
                    source.ItemId,
                    out ItemRuntimeData data) ||
                data.HasDurability)
                return;

            holdElapsed += Time.unscaledDeltaTime;

            if (holdElapsed < holdStartDelay)
                return;

            holdTakeTimer += Time.unscaledDeltaTime;

            while (holdTakeTimer >= holdTakeInterval)
            {
                holdTakeTimer -= holdTakeInterval;

                if (!hotbar.TryTakeFromStorageSlot(
                    activeHoldSlot,
                    1,
                    out ItemStack taken))
                    break;

                if (carriedStack.IsEmpty)
                {
                    carriedStack = taken;
                }
                else if (carriedStack.ItemId == taken.ItemId)
                {
                    carriedStack.Quantity += taken.Quantity;
                }
                else
                {
                    break;
                }

                holdDidTake = true;
                RefreshSlots();
                RefreshCarriedVisual();

                source = hotbar.GetStorageItemStack(activeHoldSlot);

                if (source.IsEmpty)
                    break;
            }
        }

        private void HandleSlotClick(
            int slot,
            PointerEventData.InputButton button)
        {
            if (!isOpen)
                return;

            if (suppressNextClick)
            {
                suppressNextClick = false;
                return;
            }

            if (button == PointerEventData.InputButton.Right)
            {
                HandleRightClick(slot);
            }
            else if (button == PointerEventData.InputButton.Left)
            {
                HandleLeftClick(slot);
            }

            RefreshSlots();
            RefreshCarriedVisual();
        }

        private void HandleLeftClick(int slot)
        {
            if (carriedStack.IsEmpty)
            {
                ItemStack source = hotbar.GetStorageItemStack(slot);

                if (source.IsEmpty)
                    return;

                carriedStack = source;
                carriedStackSplit = false;

                hotbar.TryTakeFromStorageSlot(
                    slot,
                    source.Quantity,
                    out _
                );

                return;
            }

            // Una pila dividida coloca solamente una unidad por clic.
            if (carriedStackSplit)
            {
                hotbar.TryPutIntoStorageSlot(
                    slot,
                    ref carriedStack,
                    true
                );

                if (carriedStack.IsEmpty)
                    carriedStackSplit = false;

                return;
            }

            ItemStack destination = hotbar.GetStorageItemStack(slot);

            if (destination.IsEmpty ||
                destination.ItemId == carriedStack.ItemId)
            {
                hotbar.TryPutIntoStorageSlot(
                    slot,
                    ref carriedStack,
                    false
                );

                return;
            }

            hotbar.TrySwapStorageSlotWithCarried(
                slot,
                ref carriedStack
            );
        }

        private void HandleRightClick(int slot)
        {
            if (!carriedStack.IsEmpty)
            {
                hotbar.TryPutIntoStorageSlot(
                    slot,
                    ref carriedStack,
                    true
                );

                if (carriedStack.IsEmpty)
                    carriedStackSplit = false;

                return;
            }

            ItemStack source = hotbar.GetStorageItemStack(slot);

            if (source.IsEmpty ||
                source.Quantity < 2 ||
                !itemRegistry.TryGetRuntimeData(
                    source.ItemId,
                    out ItemRuntimeData data) ||
                data.HasDurability)
            {
                return;
            }

            ushort amount = (ushort)(source.Quantity / 2);

            if (hotbar.TryTakeFromStorageSlot(
                slot,
                amount,
                out ItemStack taken))
            {
                carriedStack = taken;
                carriedStackSplit = true;
            }
        }

        private void UpdateCarriedVisual()
        {
            if (carriedStack.IsEmpty || Mouse.current == null)
            {
                carriedVisual.gameObject.SetActive(false);
                return;
            }

            carriedVisual.gameObject.SetActive(true);

            Canvas canvas = GetComponentInParent<Canvas>();

            if (canvas == null)
                return;

            RectTransform canvasRect = canvas.transform as RectTransform;

            if (canvasRect == null)
                return;

            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                Mouse.current.position.ReadValue(),
                eventCamera,
                out Vector2 localPoint))
            {
                carriedVisual.anchoredPosition =
                    localPoint + new Vector2(14f, -14f);
            }
        }

        private void RefreshCarriedVisual()
        {
            if (carriedStack.IsEmpty)
            {
                carriedVisual.gameObject.SetActive(false);
                return;
            }

            if (!itemRegistry.TryGetDefinition(
                carriedStack.ItemId,
                out ItemDefinition definition))
            {
                carriedVisual.gameObject.SetActive(false);
                return;
            }

            carriedIcon.texture = atlasTexture;
            carriedIcon.uvRect = GetTileUVRect(definition.Icon);
            carriedIcon.color = Color.white;

            carriedQuantityText.text =
                carriedStack.Quantity > 1
                    ? carriedStack.Quantity.ToString()
                    : "";

            ushort maxStack = 1;

            if (itemRegistry.TryGetRuntimeData(
                carriedStack.ItemId,
                out ItemRuntimeData data))
            {
                maxStack = data.HasDurability
                    ? (ushort)1
                    : data.MaxStackSize;
            }

            float fill = maxStack == 0
                ? 0f
                : Mathf.Clamp01(
                    (float)carriedStack.Quantity / maxStack
                );

            RectTransform fillRect = carriedBarFill.rectTransform;
            fillRect.anchorMax = new Vector2(fill, 1f);
            fillRect.sizeDelta = Vector2.zero;

            carriedVisual.gameObject.SetActive(true);
        }

        private void SetOpen(bool open)
        {
            isOpen = open;
            activeHoldSlot = -1;
            holdDidTake = false;

            panel.gameObject.SetActive(open);

            if (hotbarUI != null)
                hotbarUI.SetVisible(!open);

            playerController.SetInventoryOpen(open);

            if (open)
            {
                RefreshSlots();
                RefreshCarriedVisual();
            }
            else
            {
                carriedVisual.gameObject.SetActive(false);
            }
        }

        private void RefreshSlots()
        {
            for (int slot = 0; slot < 36; slot++)
            {
                ItemStack stack = hotbar.GetStorageItemStack(slot);
                RawImage icon = slotIcons[slot];

                quantityTexts[slot].text =
                    stack.IsEmpty || stack.Quantity <= 1
                        ? string.Empty
                        : stack.Quantity.ToString();

                if (stack.IsEmpty ||
                    !itemRegistry.TryGetDefinition(
                        stack.ItemId,
                        out ItemDefinition definition))
                {
                    icon.color = new Color(1f, 1f, 1f, 0f);
                    quantityTexts[slot].text = string.Empty;
                    durabilityBackgrounds[slot].gameObject.SetActive(false);
                    durabilityFills[slot].gameObject.SetActive(false);
                    continue;
                }

                icon.texture = atlasTexture;
                icon.uvRect = GetTileUVRect(definition.Icon);
                icon.color = Color.white;

                if (itemRegistry.TryGetRuntimeData(
                    stack.ItemId,
                    out ItemRuntimeData data) &&
                    data.HasDurability &&
                    data.MaxDurability > 0)
                {
                    durabilityBackgrounds[slot].gameObject.SetActive(true);
                    durabilityFills[slot].gameObject.SetActive(true);

                    float value = Mathf.Clamp01(
                        (float)stack.Durability / data.MaxDurability
                    );

                    RectTransform fill =
                        durabilityFills[slot].rectTransform;

                    fill.anchorMax = new Vector2(value, 1f);
                    fill.sizeDelta = Vector2.zero;

                    durabilityFills[slot].color =
                        value <= 0.25f ? new Color(0.9f, 0.15f, 0.1f) :
                        value <= 0.5f ? new Color(1f, 0.8f, 0.1f) :
                        new Color(0.2f, 0.9f, 0.2f);
                }
                else
                {
                    durabilityBackgrounds[slot].gameObject.SetActive(false);
                    durabilityFills[slot].gameObject.SetActive(false);
                }
            }

            lastConfigurationVersion = hotbar.ConfigurationVersion;
        }

        private RawImage CreateIcon(RectTransform slot)
        {
            GameObject obj = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(RawImage)
            );

            obj.transform.SetParent(slot, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(5f, 5f);
            rect.offsetMax = new Vector2(-5f, -5f);

            RawImage image = obj.GetComponent<RawImage>();
            image.texture = atlasTexture;
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = false;

            return image;
        }

        private Text CreateQuantity(RectTransform slot)
        {
            Text text = CreateLabel(
                string.Empty,
                slot,
                new Vector2(-3f, 2f),
                14,
                TextAnchor.LowerRight
            );

            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(2f, 2f);
            rect.offsetMax = new Vector2(-3f, -2f);

            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.raycastTarget = false;

            return text;
        }

        private void CreateDurabilityBar(
            RectTransform slot,
            int index)
        {
            GameObject bg = new GameObject(
                "DurabilityBackground",
                typeof(RectTransform),
                typeof(Image)
            );

            bg.transform.SetParent(slot, false);

            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(1f, 0f);
            bgRect.pivot = new Vector2(0.5f, 0f);
            bgRect.anchoredPosition = new Vector2(0f, 3f);
            bgRect.sizeDelta = new Vector2(-8f, 4f);

            Image bgImage = bg.GetComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.85f);
            bgImage.raycastTarget = false;

            durabilityBackgrounds[index] = bgImage;

            GameObject fillObj = new GameObject(
                "DurabilityFill",
                typeof(RectTransform),
                typeof(Image)
            );

            fillObj.transform.SetParent(bg.transform, false);

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = Vector2.zero;

            Image fill = fillObj.GetComponent<Image>();
            fill.color = new Color(0.2f, 0.9f, 0.2f);
            fill.raycastTarget = false;

            durabilityFills[index] = fill;

            bgImage.gameObject.SetActive(false);
            fill.gameObject.SetActive(false);
        }

        private Text CreateLabel(
            string value,
            Transform parent,
            Vector2 position,
            int fontSize,
            TextAnchor alignment)
        {
            GameObject obj = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(Text)
            );

            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(
                panel == null ? 300f : panel.sizeDelta.x - 20f,
                28f
            );

            Text text = obj.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;

            return text;
        }

        private Rect GetTileUVRect(AtlasTileCoordinate tile)
        {
            float width = 1f / atlasSettings.Columns;
            float height = 1f / atlasSettings.Rows;

            float x = tile.Column * width;
            float y = atlasSettings.RowZeroIsTop
                ? 1f - ((tile.Row + 1) * height)
                : tile.Row * height;

            return new Rect(x, y, width, height);
        }

        private RectTransform CreateRect(
            string objectName,
            Transform parent)
        {
            GameObject obj = new GameObject(
                objectName,
                typeof(RectTransform)
            );

            obj.transform.SetParent(parent, false);
            return obj.GetComponent<RectTransform>();
        }

        private void OnDestroy()
        {
            if (activeHoldSlot >= 0)
                activeHoldSlot = -1;
        }
    }

    public sealed class VoxelInventorySlotInput : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        private VoxelInventoryUI inventory;
        private int slotIndex;

        public void Initialize(
            VoxelInventoryUI owner,
            int index)
        {
            inventory = owner;
            slotIndex = index;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            inventory?.BeginSlotPointer(slotIndex, eventData.button);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            inventory?.EndSlotPointer(slotIndex, eventData.button);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // No cancelar el arrastre al salir: el puntero puede
            // volver a la ranura antes de soltar el botón.
        }
    }
}

using System;
using System.Collections.Generic;
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
        [SerializeField] private BlockRegistry blockRegistry;
        [SerializeField] private VoxelAtlasSettings atlasSettings;
        [SerializeField] private Texture2D atlasTexture;
        [SerializeField] private ToolRegistry toolRegistry;

        [Header("Layout")]
        [SerializeField, Min(24f)] private float slotSize = 52f;
        [SerializeField, Min(0f)] private float slotSpacing = 5f;

        [Header("Mouse Interaction")]
        [SerializeField, Min(0.05f)] private float holdStartDelay = 0.25f;
        [SerializeField, Min(0.02f)] private float holdTakeInterval = 0.08f;

        private const int Columns = 9;
        private const int Rows = 4;
        private const int InventoryRows = 3;

        private const float PanelHorizontalPadding = 16f;
        private const float TitleAreaHeight = 48f;
        private const float TopAreaGap = 18f;
        private const float BottomPadding = 24f;

        private const int CatalogColumns = 6;
        private const int CatalogRows = 5;
        private const int CatalogPageSize = CatalogColumns * CatalogRows;
        private const float CatalogPanelGap = 12f;
        private const float DoubleClickInterval = 0.3f;

        private RectTransform panel;
        private RectTransform extraSlotsPanel;
        private RectTransform toolInfoPanel;
        private Text[] toolInfoTexts;

        private Image[] slotBackgrounds;
        private RawImage[] slotIcons;
        private Text[] quantityTexts;
        private Image[] durabilityBackgrounds;
        private Image[] durabilityFills;

        private readonly List<Image> extraSlotBackgrounds =
            new List<Image>();

        private VoxelHotbarBlockPreview inventoryBlockPreview;
        private VoxelHotbarBlockPreview catalogBlockPreview;
        private VoxelHotbarBlockPreview carriedBlockPreview;

        private RectTransform carriedVisual;
        private RawImage carriedIcon;
        private Text carriedQuantityText;
        private Image carriedBarBackground;
        private Image carriedBarFill;

        private ItemStack carriedStack;
        private bool carriedStackSplit;
        private bool isOpen;
        private int selectedSlot = -1;

        private int activeHoldSlot = -1;
        private PointerEventData.InputButton activeHoldButton;
        private float holdElapsed;
        private float holdTakeTimer;
        private bool holdDidTake;
        private bool suppressNextClick;

        private int lastConfigurationVersion = -1;
        private int lastClickedSlot = -1;
        private float lastClickTime = -1f;

        private RectTransform catalogPanel;
        private Image[] catalogSlotBackgrounds;
        private RawImage[] catalogSlotIcons;
        private Text[] catalogNameTexts;

        private Button catalogPreviousButton;
        private Button catalogNextButton;
        private Text catalogPageText;

        private readonly List<ItemDefinition> catalogItems =
            new List<ItemDefinition>();

        private int catalogPage;

        private float CellSize => slotSize + slotSpacing;

        private float GridWidth =>
            Columns * slotSize + (Columns - 1) * slotSpacing;

        private float FourRowHeight =>
            Rows * slotSize + (Rows - 1) * slotSpacing;

        private void Awake()
        {
            if (hotbar == null ||
                playerController == null ||
                itemRegistry == null ||
                blockRegistry == null ||
                atlasSettings == null ||
                atlasTexture == null)
            {
                throw new InvalidOperationException(
                    "VoxelInventoryUI requiere Hotbar, PlayerController, " +
                    "ItemRegistry, BlockRegistry, VoxelAtlasSettings y atlasTexture."
                );
            }

            inventoryBlockPreview =
                new VoxelHotbarBlockPreview(atlasTexture, 36);

            catalogBlockPreview =
                new VoxelHotbarBlockPreview(atlasTexture, CatalogPageSize);

            carriedBlockPreview =
                new VoxelHotbarBlockPreview(atlasTexture, 1);

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

        // ------------------------------------------------------------
        // Construcción del inventario
        // ------------------------------------------------------------

        private void BuildUI()
        {
            panel = CreateRect("InventoryPanel", transform);

            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);

            float topAreaHeight = FourRowHeight;

            float height =
                TitleAreaHeight +
                topAreaHeight +
                TopAreaGap +
                FourRowHeight +
                BottomPadding;

            panel.sizeDelta = new Vector2(
                GridWidth + PanelHorizontalPadding * 2f,
                height
            );

            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.075f, 0.085f, 0.08f, 0.98f);

            Text title = CreateLabel(
                "INVENTARIO",
                panel,
                new Vector2(0f, -20f),
                20,
                TextAnchor.MiddleCenter
            );

            title.rectTransform.sizeDelta = new Vector2(
                panel.sizeDelta.x - 20f,
                32f
            );

            slotBackgrounds = new Image[36];
            slotIcons = new RawImage[36];
            quantityTexts = new Text[36];
            durabilityBackgrounds = new Image[36];
            durabilityFills = new Image[36];

            // El panel superior ocupa las primeras cuatro filas visuales.
            // Debajo se muestran las 27 ranuras del inventario y la hotbar.
            float startX = -GridWidth * 0.5f;

            float inventoryTop =
                height * 0.5f -
                TitleAreaHeight -
                topAreaHeight -
                TopAreaGap;

            for (int displayIndex = 0; displayIndex < 36; displayIndex++)
            {
                // Las primeras 27 ranuras visuales son las ranuras 9-35.
                // La última fila visual es la hotbar, ranuras 0-8.
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
                    startX +
                    column * CellSize +
                    slotSize * 0.5f,
                    inventoryTop -
                    row * CellSize -
                    slotSize * 0.5f
                );

                slot.sizeDelta = new Vector2(slotSize, slotSize);

                Image slotBackground =
                    slot.gameObject.AddComponent<Image>();

                slotBackground.color =
                    new Color(0.19f, 0.21f, 0.20f, 1f);

                Button button = slot.gameObject.AddComponent<Button>();
                button.targetGraphic = slotBackground;
                button.transition = Selectable.Transition.ColorTint;

                VoxelInventorySlotInput input =
                    slot.gameObject.AddComponent<VoxelInventorySlotInput>();

                input.Initialize(this, slotIndex);

                slotBackgrounds[slotIndex] = slotBackground;
                slotIcons[slotIndex] = CreateIcon(slot);
                quantityTexts[slotIndex] = CreateQuantity(slot);

                CreateDurabilityBar(slot, slotIndex);
            }

            BuildExtraSlots();
            BuildToolInfoPanel();
            BuildCreativeCatalog();

            RefreshSlots();

            catalogPanel.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------
        // Panel superior: botones, jugador, botones, información
        // ------------------------------------------------------------

        private void BuildExtraSlots()
        {
            extraSlotsPanel = CreateRect("InventoryTopArea", panel);

            extraSlotsPanel.anchorMin = new Vector2(0.5f, 1f);
            extraSlotsPanel.anchorMax = new Vector2(0.5f, 1f);
            extraSlotsPanel.pivot = new Vector2(0.5f, 1f);

            extraSlotsPanel.sizeDelta = new Vector2(
                GridWidth,
                FourRowHeight
            );

            // Colocar el área superior debajo del título.
            extraSlotsPanel.anchoredPosition = new Vector2(
                253f, /* Esto esta bien, no lo mueban */
                -TitleAreaHeight
            );

            float startX = -GridWidth * 0.5f;

            // Columna izquierda: cuatro espacios para botones.
            CreateDecorativeSlotColumn(startX, 4, false);

            // Área del jugador: columnas 2 y 3.
            CreateJoinedPanel(
                "PlayerEquipmentArea",
                startX + CellSize,
                0f,
                2f * slotSize + slotSpacing,
                FourRowHeight
            );

            // Segunda columna de botones.
            CreateDecorativeSlotColumn(
                startX + 3f * CellSize,
                4,
                false
            );

            // Panel de información: columnas 5 a 9.
            CreateJoinedPanel(
                "ToolInformationArea",
                startX + 4f * CellSize,
                0f,
                5f * slotSize + 4f * slotSpacing,
                FourRowHeight
            );
        }

        private void CreateJoinedPanel(
            string objectName,
            float x,
            float y,
            float width,
            float height)
        {
            RectTransform rect = CreateRect(
                objectName,
                extraSlotsPanel
            );

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);

            Image background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.14f, 0.13f, 1f);
            background.raycastTarget = false;
        }

        private void CreateDecorativeSlotColumn(
            float x,
            int count,
            bool separated)
        {
            for (int i = 0; i < count; i++)
            {
                CreateDecorativeSlot(
                    new Vector2(x, -i * CellSize),
                    $"FutureColumnSlot_{i}"
                );
            }
        }

        private void CreateDecorativeSlotGrid(
            float x,
            int columns,
            int rows)
        {
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    CreateDecorativeSlot(
                        new Vector2(
                            x + column * CellSize,
                            -row * CellSize
                        ),
                        $"FutureGridSlot_{row}_{column}"
                    );
                }
            }
        }

        private void CreateDecorativeSlotRow(
            float x,
            float y,
            int count)
        {
            for (int i = 0; i < count; i++)
            {
                CreateDecorativeSlot(
                    new Vector2(x + i * CellSize, y),
                    $"FutureRowSlot_{i}"
                );
            }
        }

        private void CreateDecorativeSlot(
            Vector2 position,
            string objectName)
        {
            RectTransform slot = CreateRect(
                objectName,
                extraSlotsPanel
            );

            slot.anchorMin = new Vector2(0f, 1f);
            slot.anchorMax = new Vector2(0f, 1f);
            slot.pivot = new Vector2(0f, 1f);

            slot.anchoredPosition = position;
            slot.sizeDelta = new Vector2(slotSize, slotSize);

            Image background = slot.gameObject.AddComponent<Image>();
            background.color = new Color(0.16f, 0.18f, 0.17f, 1f);
            background.raycastTarget = false;

            extraSlotBackgrounds.Add(background);
        }

        // ------------------------------------------------------------
        // Información de herramientas dentro del panel superior
        // ------------------------------------------------------------

        private void BuildToolInfoPanel()
        {
            Transform infoTransform =
                extraSlotsPanel.Find("ToolInformationArea");

            if (infoTransform == null)
            {
                Debug.LogError(
                    "No existe ToolInformationArea dentro de InventoryTopArea.",
                    this
                );
                return;
            }

            toolInfoPanel = infoTransform as RectTransform;
            toolInfoTexts = new Text[12];

            Text title = CreateLabel(
                "DATOS DE HERRAMIENTA",
                toolInfoPanel,
                new Vector2(0f, -18f),
                15,
                TextAnchor.MiddleCenter
            );

            title.rectTransform.sizeDelta = new Vector2(
                toolInfoPanel.sizeDelta.x - 12f,
                28f
            );

            float top = -42f;
            float rowHeight = 14f;
            float columnWidth = toolInfoPanel.sizeDelta.x - 20f;

            for (int i = 0; i < toolInfoTexts.Length; i++)
            {
                Text label = CreateLabel(
                    "",
                    toolInfoPanel,
                    new Vector2(0f, top - i * rowHeight),
                    11,
                    TextAnchor.MiddleLeft
                );

                label.rectTransform.sizeDelta = new Vector2(
                    columnWidth,
                    rowHeight
                );

                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;

                toolInfoTexts[i] = label;
            }

            SetToolInfoMessage("Seleccioná una herramienta");
        }

        private void SetToolInfoMessage(string message)
        {
            if (toolInfoTexts == null)
                return;

            for (int i = 0; i < toolInfoTexts.Length; i++)
                toolInfoTexts[i].text = i == 0 ? message : "";
        }

        private void RefreshToolInfo(int slot)
        {
            if (toolInfoTexts == null)
                return;

            ItemStack stack = hotbar.GetStorageItemStack(slot);

            if (stack.IsEmpty ||
                !itemRegistry.TryGetDefinition(
                    stack.ItemId,
                    out ItemDefinition definition))
            {
                SetToolInfoMessage("Ranura vacía");
                return;
            }

            if (definition.ToolDefinition == null)
            {
                SetToolInfoMessage("El objeto no es una herramienta");
                return;
            }

            if (toolRegistry == null)
            {
                SetToolInfoMessage("Falta asignar ToolRegistry");
                return;
            }

            ushort toolId = definition.ToolDefinition.Id;

            if (!toolRegistry.TryGetRuntimeData(
                    toolId,
                    out ToolRuntimeData data) ||
                !data.IsValid)
            {
                SetToolInfoMessage(
                    "Datos de herramienta no disponibles"
                );
                return;
            }

            toolInfoTexts[0].text = definition.ItemName;
            toolInfoTexts[1].text = $"Tipo: {data.ToolType}";
            toolInfoTexts[2].text = $"Daño base: {data.BaseDamage:0.##}";
            toolInfoTexts[3].text = $"Velocidad base: {data.BaseSpeed:0.##}";
            toolInfoTexts[4].text = $"Eficiencia: {data.BaseEfficiency:0.##}";
            toolInfoTexts[5].text = $"Dureza cabeza: {data.HeadHardness:0.##}";
            toolInfoTexts[6].text = $"Resistencia cabeza: {data.HeadResistance:0.##}";
            toolInfoTexts[7].text = $"Tenacidad cabeza: {data.HeadToughness:0.##}";
            toolInfoTexts[8].text = $"Retención de filo: {data.EdgeRetention:0.##}";
            toolInfoTexts[9].text = $"Aptitud de filo: {data.EdgeAptitude:0.##}";
            toolInfoTexts[10].text = $"Peso total: {data.TotalWeight:0.##}";
            toolInfoTexts[11].text =
                $"Durabilidad base: {data.BaseHeadDurability:0.##} / " +
                $"{data.BaseHandleDurability:0.##}";
        }

        // ------------------------------------------------------------
        // Catálogo creativo
        // ------------------------------------------------------------

        private void BuildCreativeCatalog()
        {
            float width =
                CatalogColumns * slotSize +
                (CatalogColumns - 1) * slotSpacing + 32f;

            float height =
                CatalogRows * slotSize +
                (CatalogRows - 1) * slotSpacing + 76f;

            catalogPanel = CreateRect(
                "CreativeCatalogPanel",
                transform
            );

            catalogPanel.anchorMin = new Vector2(0.5f, 0.5f);
            catalogPanel.anchorMax = new Vector2(0.5f, 0.5f);
            catalogPanel.pivot = new Vector2(0.5f, 0.5f);
            catalogPanel.sizeDelta = new Vector2(width, height);

            Image background = catalogPanel.gameObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.08f, 0.08f, 0.96f);

            Text catalogTitle = CreateLabel(
                "CATÁLOGO CREATIVO",
                catalogPanel,
                new Vector2(0f, -19f),
                18,
                TextAnchor.MiddleCenter
            );

            catalogTitle.rectTransform.sizeDelta =
                new Vector2(width - 20f, 28f);

            catalogSlotBackgrounds = new Image[CatalogPageSize];
            catalogSlotIcons = new RawImage[CatalogPageSize];
            catalogNameTexts = new Text[CatalogPageSize];

            float startX = -width * 0.5f + 16f;
            float startY = height * 0.5f - 58f;

            for (int i = 0; i < CatalogPageSize; i++)
            {
                int column = i % CatalogColumns;
                int row = i / CatalogColumns;

                RectTransform slot = CreateRect(
                    $"CatalogSlot_{i}",
                    catalogPanel
                );

                slot.anchorMin = new Vector2(0.5f, 0.5f);
                slot.anchorMax = new Vector2(0.5f, 0.5f);
                slot.pivot = new Vector2(0.5f, 0.5f);

                slot.anchoredPosition = new Vector2(
                    startX + column * CellSize + slotSize * 0.5f,
                    startY - row * CellSize - slotSize * 0.5f
                );

                slot.sizeDelta = new Vector2(slotSize, slotSize);

                Image slotBackground =
                    slot.gameObject.AddComponent<Image>();

                slotBackground.color =
                    new Color(0.20f, 0.20f, 0.20f, 1f);

                Button button = slot.gameObject.AddComponent<Button>();
                button.targetGraphic = slotBackground;
                button.transition = Selectable.Transition.None;

                int capturedIndex = i;
                button.onClick.AddListener(
                    () => HandleCatalogSlotClick(capturedIndex)
                );

                RawImage icon = CreateIcon(slot);
                icon.rectTransform.offsetMin = new Vector2(10.5f, 17f);
                icon.rectTransform.offsetMax = new Vector2(-10.5f, -4f);

                Text itemName = CreateLabel(
                    "",
                    slot,
                    Vector2.zero,
                    9,
                    TextAnchor.MiddleCenter
                );

                RectTransform nameRect = itemName.rectTransform;
                nameRect.anchorMin = new Vector2(0f, 0f);
                nameRect.anchorMax = new Vector2(1f, 0f);
                nameRect.pivot = new Vector2(0.5f, 0f);
                nameRect.anchoredPosition = new Vector2(0f, 1f);
                nameRect.sizeDelta = new Vector2(-4f, 15f);

                itemName.fontSize = 9;
                itemName.horizontalOverflow = HorizontalWrapMode.Wrap;
                itemName.verticalOverflow = VerticalWrapMode.Truncate;

                catalogSlotBackgrounds[i] = slotBackground;
                catalogSlotIcons[i] = icon;
                catalogNameTexts[i] = itemName;
            }

            float buttonY = -height * 0.5f + 21f;

            catalogPreviousButton = CreateCatalogButton(
                "PreviousPage",
                "<",
                new Vector2(-width * 0.25f, buttonY),
                () => ChangeCatalogPage(-1)
            );

            catalogNextButton = CreateCatalogButton(
                "NextPage",
                ">",
                new Vector2(width * 0.25f, buttonY),
                () => ChangeCatalogPage(1)
            );

            catalogPageText = CreateLabel(
                "",
                catalogPanel,
                new Vector2(0f, buttonY),
                12,
                TextAnchor.MiddleCenter
            );

            catalogPageText.rectTransform.sizeDelta =
                new Vector2(100f, 28f);

            LoadCreativeCatalog();
            RefreshCreativeCatalog();
            catalogPanel.gameObject.SetActive(false);
        }

        private Button CreateCatalogButton(
            string objectName,
            string label,
            Vector2 position,
            Action callback)
        {
            RectTransform rect = CreateRect(
                objectName,
                catalogPanel
            );

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(42f, 28f);

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.28f, 0.28f, 0.28f, 1f);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() => callback());

            Text text = CreateLabel(
                label,
                rect,
                Vector2.zero,
                16,
                TextAnchor.MiddleCenter
            );

            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            return button;
        }

        private void LoadCreativeCatalog()
        {
            catalogItems.Clear();

            foreach (ItemDefinition definition in itemRegistry.Definitions)
            {
                if (definition == null || definition.Id == 0)
                    continue;

                catalogItems.Add(definition);
            }

            catalogPage = Mathf.Clamp(
                catalogPage,
                0,
                Mathf.Max(0, (catalogItems.Count - 1) / CatalogPageSize)
            );
        }

        private void RefreshCreativeCatalog()
        {
            if (catalogSlotIcons == null)
                return;

            int pageCount = Mathf.Max(
                1,
                Mathf.CeilToInt(
                    (float)catalogItems.Count / CatalogPageSize
                )
            );

            catalogPage = Mathf.Clamp(catalogPage, 0, pageCount - 1);

            int firstItem = catalogPage * CatalogPageSize;

            for (int i = 0; i < CatalogPageSize; i++)
            {
                int itemIndex = firstItem + i;
                bool hasItem = itemIndex < catalogItems.Count;

                catalogSlotBackgrounds[i].gameObject.SetActive(hasItem);

                if (!hasItem)
                {
                    catalogBlockPreview.ClearBlock(i);
                    catalogSlotIcons[i].color =
                        new Color(1f, 1f, 1f, 0f);

                    catalogNameTexts[i].text = "";
                    continue;
                }

                ItemDefinition definition = catalogItems[itemIndex];

                RefreshItemIcon(
                    catalogSlotIcons[i],
                    definition,
                    i,
                    catalogBlockPreview
                );

                catalogNameTexts[i].text = definition.ItemName;
            }

            catalogPageText.text = $"{catalogPage + 1} / {pageCount}";
            catalogPreviousButton.interactable = catalogPage > 0;
            catalogNextButton.interactable = catalogPage < pageCount - 1;
        }

        private void ChangeCatalogPage(int direction)
        {
            int pageCount = Mathf.Max(
                1,
                Mathf.CeilToInt(
                    (float)catalogItems.Count / CatalogPageSize
                )
            );

            catalogPage = Mathf.Clamp(
                catalogPage + direction,
                0,
                pageCount - 1
            );

            RefreshCreativeCatalog();
        }

        private void HandleCatalogSlotClick(int catalogSlot)
        {
            if (!isOpen)
                return;

            // Soltar una pila sobre el catálogo descarta esa pila.
            if (!carriedStack.IsEmpty)
            {
                carriedStack.Clear();
                carriedStackSplit = false;

                RefreshCarriedVisual();
                RefreshSlots();
                return;
            }

            GiveCreativeItemAt(catalogSlot);
        }

        private bool TryReturnCarriedStack()
        {
            if (carriedStack.IsEmpty)
                return true;

            for (int i = 0; i < VoxelPlayerHotbar.TotalSlotCount; i++)
            {
                if (hotbar.TryPutIntoStorageSlot(
                    i,
                    ref carriedStack,
                    false))
                {
                    if (carriedStack.IsEmpty)
                    {
                        carriedStackSplit = false;
                        return true;
                    }
                }
            }

            RefreshSlots();
            RefreshCarriedVisual();
            return carriedStack.IsEmpty;
        }

        private void GiveCreativeItemAt(int catalogSlot)
        {
            if (!isOpen ||
                playerController.CurrentMode != VoxelPlayerMode.Creative)
            {
                return;
            }

            int itemIndex = catalogPage * CatalogPageSize + catalogSlot;

            if (itemIndex < 0 || itemIndex >= catalogItems.Count)
                return;

            ItemDefinition definition = catalogItems[itemIndex];

            if (!itemRegistry.TryGetRuntimeData(
                definition.Id,
                out ItemRuntimeData data))
            {
                return;
            }

            ushort quantity = data.HasDurability
                ? (ushort)1
                : data.MaxStackSize;

            if (quantity == 0)
                return;

            hotbar.TryAddItem(definition.Id, quantity);
        }

        // ------------------------------------------------------------
        // Objeto que sigue al cursor
        // ------------------------------------------------------------

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

            carriedQuantityText.rectTransform.anchorMin =
                new Vector2(0f, 0f);

            carriedQuantityText.rectTransform.anchorMax =
                new Vector2(1f, 0f);

            carriedQuantityText.rectTransform.pivot =
                new Vector2(0.5f, 0f);

            carriedQuantityText.rectTransform.anchoredPosition =
                new Vector2(0f, 13f);

            carriedQuantityText.rectTransform.sizeDelta =
                new Vector2(0f, 18f);

            GameObject barBackground = new GameObject(
                "CarriedQuantityBackground",
                typeof(RectTransform),
                typeof(Image)
            );

            barBackground.transform.SetParent(carriedVisual, false);

            RectTransform bgRect =
                barBackground.GetComponent<RectTransform>();

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

            RectTransform fillRect =
                barFill.GetComponent<RectTransform>();

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

            if (button == PointerEventData.InputButton.Left)
            {
                float now = Time.unscaledTime;

                bool isDoubleClick =
                    lastClickedSlot == slot &&
                    now - lastClickTime <= DoubleClickInterval;

                lastClickedSlot = slot;
                lastClickTime = now;

                if (isDoubleClick)
                {
                    lastClickedSlot = -1;
                    lastClickTime = -1f;

                    if (!carriedStack.IsEmpty)
                    {
                        hotbar.TryPutIntoStorageSlot(
                            slot,
                            ref carriedStack,
                            false
                        );
                    }

                    carriedStackSplit = false;
                    ConsolidateItemStacks(slot);
                    RefreshSlots();
                    RefreshCarriedVisual();
                    return;
                }
            }

            if (button == PointerEventData.InputButton.Left)
            {
                selectedSlot = slot;

                if (isOpen)
                    RefreshToolInfo(slot);
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

            bool pressed =
                activeHoldButton == PointerEventData.InputButton.Left
                    ? Mouse.current.leftButton.isPressed
                    : Mouse.current.rightButton.isPressed;

            if (!pressed)
                return;

            if (activeHoldButton != PointerEventData.InputButton.Left ||
                !carriedStack.IsEmpty)
            {
                return;
            }

            ItemStack source =
                hotbar.GetStorageItemStack(activeHoldSlot);

            if (source.IsEmpty ||
                !itemRegistry.TryGetRuntimeData(
                    source.ItemId,
                    out ItemRuntimeData data) ||
                data.HasDurability)
            {
                return;
            }

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
                {
                    break;
                }

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

        // ------------------------------------------------------------
        // Interacción con ranuras
        // ------------------------------------------------------------

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
                HandleRightClick(slot);
            else if (button == PointerEventData.InputButton.Left)
                HandleLeftClick(slot);

            RefreshSlots();
            RefreshCarriedVisual();
        }

        private void ConsolidateItemStacks(int targetSlot)
        {
            ItemStack target = hotbar.GetStorageItemStack(targetSlot);

            if (target.IsEmpty ||
                !itemRegistry.TryGetRuntimeData(
                    target.ItemId,
                    out ItemRuntimeData data) ||
                data.HasDurability ||
                data.MaxStackSize <= 1)
            {
                return;
            }

            ushort itemId = target.ItemId;
            ushort maxStack = data.MaxStackSize;

            for (int i = 0; i < VoxelPlayerHotbar.TotalSlotCount; i++)
            {
                if (i == targetSlot)
                    continue;

                ItemStack source = hotbar.GetStorageItemStack(i);

                if (source.IsEmpty || source.ItemId != itemId)
                    continue;

                target = hotbar.GetStorageItemStack(targetSlot);

                if (target.Quantity >= maxStack)
                    break;

                ushort available = (ushort)(maxStack - target.Quantity);
                ushort amount = (ushort)Mathf.Min(
                    source.Quantity,
                    available
                );

                if (!hotbar.TryTakeFromStorageSlot(
                    i,
                    amount,
                    out ItemStack taken))
                {
                    continue;
                }

                if (!hotbar.TryPutIntoStorageSlot(
                    targetSlot,
                    ref taken,
                    false))
                {
                    hotbar.TryPutIntoStorageSlot(
                        i,
                        ref taken,
                        false
                    );
                }
            }
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

        // ------------------------------------------------------------
        // Visual del objeto en el cursor
        // ------------------------------------------------------------

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

            RectTransform canvasRect =
                canvas.transform as RectTransform;

            if (canvasRect == null)
                return;

            Camera eventCamera =
                canvas.renderMode == RenderMode.ScreenSpaceOverlay
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

            RefreshItemIcon(
                carriedIcon,
                definition,
                0,
                carriedBlockPreview
            );

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

        // ------------------------------------------------------------
        // Abrir y cerrar
        // ------------------------------------------------------------

        private void SetOpen(bool open)
        {
            if (!open && !TryReturnCarriedStack())
            {
                Debug.LogWarning(
                    "Inventario lleno: no se puede cerrar porque hay un objeto en el cursor.",
                    this
                );

                return;
            }

            isOpen = open;
            activeHoldSlot = -1;
            holdDidTake = false;
            holdElapsed = 0f;
            holdTakeTimer = 0f;

            bool creative =
                playerController.CurrentMode == VoxelPlayerMode.Creative;

            float inventoryWidth = panel.sizeDelta.x;
            float catalogWidth = catalogPanel.sizeDelta.x;

            if (creative)
            {
                // Centra el conjunto catálogo + inventario.
                float inventoryX =
                    (catalogWidth + CatalogPanelGap) * 0.5f;

                panel.anchoredPosition = new Vector2(
                    inventoryX,
                    0f
                );

                catalogPanel.anchoredPosition = new Vector2(
                    inventoryX -
                    inventoryWidth * 0.5f -
                    CatalogPanelGap -
                    catalogWidth * 0.5f,
                    0f
                );
            }
            else
            {
                panel.anchoredPosition = Vector2.zero;
            }

            // El panel superior y la información son hijos del inventario.
            // No se posicionan ni se activan por separado.
            panel.gameObject.SetActive(open);
            catalogPanel.gameObject.SetActive(open && creative);

            if (hotbarUI != null)
                hotbarUI.SetVisible(!open);

            playerController.SetInventoryOpen(open);

            if (open)
            {
                RefreshSlots();

                if (selectedSlot >= 0)
                    RefreshToolInfo(selectedSlot);
                else
                    SetToolInfoMessage("Seleccioná una herramienta");

                if (creative)
                {
                    LoadCreativeCatalog();
                    RefreshCreativeCatalog();
                }

                RefreshCarriedVisual();
            }
            else
            {
                carriedVisual.gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------
        // Iconos de bloques e ítems
        // ------------------------------------------------------------

        private void RefreshItemIcon(
            RawImage image,
            ItemDefinition definition,
            int previewSlot,
            VoxelHotbarBlockPreview preview)
        {
            preview.ClearBlock(previewSlot);

            image.texture = atlasTexture;
            image.uvRect = new Rect(0f, 0f, 1f, 1f);

            if (!definition.RepresentsBlock)
            {
                image.uvRect = GetTileUVRect(definition.Icon);
                image.color = Color.white;
                return;
            }

            ushort blockId = definition.BlockId;

            if (blockId == BlockIds.Air)
            {
                image.color = new Color(1f, 1f, 1f, 0f);
                return;
            }

            if (!blockRegistry.TryGetDefinition(
                blockId,
                out BlockDefinition blockDefinition))
            {
                image.color = Color.magenta;
                return;
            }

            if (preview.SetBlock(
                previewSlot,
                blockDefinition,
                atlasSettings))
            {
                image.texture = preview.Texture;
                image.uvRect = preview.GetUVRect(previewSlot);
                image.color = Color.white;
                return;
            }

            // Respaldo 2D para bloques que no sean cubos ni fluidos.
            image.uvRect = GetTileUVRect(blockDefinition.SideTexture);
            image.color = Color.white;
        }

        private void RefreshSlots()
        {
            for (int slot = 0; slot < 36; slot++)
            {
                ItemStack stack = hotbar.GetStorageItemStack(slot);
                RawImage icon = slotIcons[slot];

                inventoryBlockPreview.ClearBlock(slot);

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

                RefreshItemIcon(
                    icon,
                    definition,
                    slot,
                    inventoryBlockPreview
                );

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
                        value <= 0.25f
                            ? new Color(0.9f, 0.15f, 0.1f)
                            : value <= 0.5f
                                ? new Color(1f, 0.8f, 0.1f)
                                : new Color(0.2f, 0.9f, 0.2f);
                }
                else
                {
                    durabilityBackgrounds[slot].gameObject.SetActive(false);
                    durabilityFills[slot].gameObject.SetActive(false);
                }
            }

            lastConfigurationVersion = hotbar.ConfigurationVersion;
        }

        // ------------------------------------------------------------
        // Elementos de interfaz reutilizables
        // ------------------------------------------------------------

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

            RectTransform fillRect =
                fillObj.GetComponent<RectTransform>();

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
            inventoryBlockPreview?.Dispose();
            catalogBlockPreview?.Dispose();
            carriedBlockPreview?.Dispose();

            inventoryBlockPreview = null;
            catalogBlockPreview = null;
            carriedBlockPreview = null;

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
            // No cancelar el arrastre al salir de la ranura.
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

namespace WildEarth.Voxel
{
    public sealed class VoxelHotbarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private VoxelPlayerHotbar hotbar;

        [SerializeField]
        private ItemRegistry itemRegistry;

        [SerializeField]
        private BlockRegistry blockRegistry;

        [SerializeField]
        private VoxelAtlasSettings atlasSettings;

        [SerializeField]
        private Texture2D atlasTexture;

        [Header("Layout")]
        [SerializeField, Min(1f)]
        private float slotSize = 64f;

        [SerializeField, Min(0f)]
        private float slotSpacing = 4f;

        [SerializeField, Min(0f)]
        private float bottomOffset = 24f;

        [Header("Visual")]
        [SerializeField, Min(1f)]
        private float selectionBorder = 4f;

        [Header("Quantity")]
        [SerializeField, Min(1f)]
        private float quantityFontSize = 20f;

        [SerializeField]
        private Color quantityColor = Color.white;

        [Header("Durability")]
        [SerializeField, Min(1f)]
        private float durabilityBarHeight = 5f;

        [SerializeField, Min(0f)]
        private float durabilityBarHorizontalPadding = 5f;

        [SerializeField, Min(0f)]
        private float durabilityBarBottomPadding = 3f;

        [SerializeField]
        private Color durabilityBackgroundColor =
            new Color(0f, 0f, 0f, 0.75f);

        [SerializeField]
        private Color durabilityFullColor =
            new Color(0.2f, 0.9f, 0.2f, 1f);

        [SerializeField]
        private Color durabilityMediumColor =
            new Color(1f, 0.8f, 0.1f, 1f);

        [SerializeField]
        private Color durabilityLowColor =
            new Color(0.9f, 0.15f, 0.1f, 1f);

        private RectTransform hotbarRoot;

        private RawImage[] slotImages;

        private Image[] selectionImages;

        private Text[] quantityTexts;

        private Image[] durabilityBackgrounds;

        private Image[] durabilityFills;

        private Texture2D generatedWhiteTexture;

        private bool initialized;

        private int lastConfigurationVersion = -1;

        private void Awake()
        {
            ValidateReferences();

            BuildHotbar();
            RefreshAllSlots();

            lastConfigurationVersion =
                hotbar.ConfigurationVersion;

            initialized = true;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            if (lastConfigurationVersion !=
                hotbar.ConfigurationVersion)
            {
                RefreshAllSlots();

                lastConfigurationVersion =
                    hotbar.ConfigurationVersion;

                return;
            }

            UpdateSelection();
            RefreshQuantities();
            RefreshDurabilityBars();
        }

        private void ValidateReferences()
        {
            if (hotbar == null)
            {
                throw new System.InvalidOperationException(
                    "VoxelHotbarUI requiere VoxelPlayerHotbar."
                );
            }

            if (itemRegistry == null)
            {
                throw new System.InvalidOperationException(
                    "VoxelHotbarUI requiere ItemRegistry."
                );
            }

            if (blockRegistry == null)
            {
                throw new System.InvalidOperationException(
                    "VoxelHotbarUI requiere BlockRegistry."
                );
            }

            if (atlasSettings == null)
            {
                throw new System.InvalidOperationException(
                    "VoxelHotbarUI requiere VoxelAtlasSettings."
                );
            }

            if (atlasTexture == null)
            {
                throw new System.InvalidOperationException(
                    "VoxelHotbarUI requiere la textura del atlas."
                );
            }
        }

        private void BuildHotbar()
        {
            hotbarRoot =
                CreateRectTransform(
                    "Hotbar",
                    transform
                );

            hotbarRoot.anchorMin =
                new Vector2(0.5f, 0f);

            hotbarRoot.anchorMax =
                new Vector2(0.5f, 0f);

            hotbarRoot.pivot =
                new Vector2(0.5f, 0f);

            float width =
                (VoxelPlayerHotbar.SlotCount * slotSize) +
                ((VoxelPlayerHotbar.SlotCount - 1) *
                 slotSpacing);

            hotbarRoot.sizeDelta =
                new Vector2(
                    width,
                    slotSize
                );

            hotbarRoot.anchoredPosition =
                new Vector2(
                    0f,
                    bottomOffset
                );

            slotImages =
                new RawImage[
                    VoxelPlayerHotbar.SlotCount
                ];

            selectionImages =
                new Image[
                    VoxelPlayerHotbar.SlotCount
                ];

            quantityTexts =
                new Text[
                    VoxelPlayerHotbar.SlotCount
                ];

            durabilityBackgrounds =
                new Image[
                    VoxelPlayerHotbar.SlotCount
                ];

            durabilityFills =
                new Image[
                    VoxelPlayerHotbar.SlotCount
                ];

            for (int i = 0;
                 i < VoxelPlayerHotbar.SlotCount;
                 i++)
            {
                CreateSlot(i);
            }
        }

        private void CreateSlot(int index)
        {
            RectTransform slot =
                CreateRectTransform(
                    $"Slot_{index + 1}",
                    hotbarRoot
                );

            slot.anchorMin =
                new Vector2(0f, 0.5f);

            slot.anchorMax =
                new Vector2(0f, 0.5f);

            slot.pivot =
                new Vector2(0f, 0.5f);

            slot.sizeDelta =
                new Vector2(
                    slotSize,
                    slotSize
                );

            float x =
                index *
                (slotSize + slotSpacing);

            slot.anchoredPosition =
                new Vector2(
                    x,
                    0f
                );

            RawImage background =
                slot.gameObject.AddComponent<RawImage>();

            background.texture =
                GetWhiteTexture();

            background.color =
                new Color(
                    0.05f,
                    0.05f,
                    0.05f,
                    0.85f
                );

            slotImages[index] =
                CreateBlockImage(slot);

            quantityTexts[index] =
                CreateQuantityText(slot);

            CreateDurabilityBar(
                slot,
                index
            );

            selectionImages[index] =
                CreateSelectionBorder(slot);

            selectionImages[index]
                .transform
                .SetAsLastSibling();
        }

        private RawImage CreateBlockImage(
            RectTransform slot)
        {
            GameObject objectInstance =
                new GameObject(
                    "BlockIcon",
                    typeof(RectTransform),
                    typeof(RawImage)
                );

            objectInstance.transform.SetParent(
                slot,
                false
            );

            RectTransform rect =
                objectInstance.GetComponent<RectTransform>();

            rect.anchorMin =
                Vector2.zero;

            rect.anchorMax =
                Vector2.one;

            rect.offsetMin =
                new Vector2(
                    6f,
                    6f
                );

            rect.offsetMax =
                new Vector2(
                    -6f,
                    -6f
                );

            RawImage image =
                objectInstance.GetComponent<RawImage>();

            image.texture =
                atlasTexture;

            image.raycastTarget =
                false;

            return image;
        }

        private Text CreateQuantityText(
            RectTransform slot)
        {
            GameObject objectInstance =
                new GameObject(
                    "Quantity",
                    typeof(RectTransform),
                    typeof(Text)
                );

            objectInstance.transform.SetParent(
                slot,
                false
            );

            RectTransform rect =
                objectInstance.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(1f, 0f);

            rect.anchorMax =
                new Vector2(1f, 0f);

            rect.pivot =
                new Vector2(1f, 0f);

            rect.anchoredPosition =
                new Vector2(
                    -4f,
                    3f
                );

            rect.sizeDelta =
                new Vector2(
                    slotSize * 0.6f,
                    slotSize * 0.45f
                );

            Text text =
                objectInstance.GetComponent<Text>();

            text.text =
                string.Empty;

            text.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf"
                );

            text.fontSize =
                Mathf.RoundToInt(quantityFontSize);

            text.fontStyle =
                FontStyle.Bold;

            text.alignment =
                TextAnchor.LowerRight;

            text.horizontalOverflow =
                HorizontalWrapMode.Overflow;

            text.verticalOverflow =
                VerticalWrapMode.Overflow;

            text.color =
                quantityColor;

            text.raycastTarget =
                false;

            return text;
        }

        private void CreateDurabilityBar(
            RectTransform slot,
            int index)
        {
            GameObject backgroundObject =
                new GameObject(
                    "DurabilityBackground",
                    typeof(RectTransform),
                    typeof(Image)
                );

            backgroundObject.transform.SetParent(
                slot,
                false
            );

            RectTransform backgroundRect =
                backgroundObject.GetComponent<RectTransform>();

            backgroundRect.anchorMin =
                new Vector2(0f, 0f);

            backgroundRect.anchorMax =
                new Vector2(1f, 0f);

            backgroundRect.pivot =
                new Vector2(0.5f, 0f);

            backgroundRect.anchoredPosition =
                new Vector2(
                    0f,
                    durabilityBarBottomPadding
                );

            backgroundRect.sizeDelta =
                new Vector2(
                    -durabilityBarHorizontalPadding * 2f,
                    durabilityBarHeight
                );

            Image background =
                backgroundObject.GetComponent<Image>();

            background.color =
                durabilityBackgroundColor;

            background.raycastTarget =
                false;

            durabilityBackgrounds[index] =
                background;

            GameObject fillObject =
                new GameObject(
                    "DurabilityFill",
                    typeof(RectTransform),
                    typeof(Image)
                );

            fillObject.transform.SetParent(
                backgroundObject.transform,
                false
            );

            RectTransform fillRect =
                fillObject.GetComponent<RectTransform>();

            fillRect.anchorMin =
                new Vector2(0f, 0f);

            fillRect.anchorMax =
                new Vector2(0f, 1f);

            fillRect.pivot =
                new Vector2(0f, 0.5f);

            fillRect.anchoredPosition =
                Vector2.zero;

            fillRect.sizeDelta =
                Vector2.zero;

            Image fill =
                fillObject.GetComponent<Image>();

            fill.color =
                durabilityFullColor;

            fill.raycastTarget =
                false;

            durabilityFills[index] =
                fill;
        }

        private Image CreateSelectionBorder(
            RectTransform slot)
        {
            GameObject objectInstance =
                new GameObject(
                    "Selection",
                    typeof(RectTransform),
                    typeof(Image)
                );

            objectInstance.transform.SetParent(
                slot,
                false
            );

            RectTransform rect =
                objectInstance.GetComponent<RectTransform>();

            rect.anchorMin =
                Vector2.zero;

            rect.anchorMax =
                Vector2.one;

            rect.offsetMin =
                Vector2.zero;

            rect.offsetMax =
                Vector2.zero;

            Image image =
                objectInstance.GetComponent<Image>();

            image.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    0f
                );

            image.raycastTarget =
                false;

            return image;
        }

        private void RefreshAllSlots()
        {
            for (int i = 0;
                 i < VoxelPlayerHotbar.SlotCount;
                 i++)
            {
                RefreshSlot(i);
            }

            UpdateSelection();
        }

        private void RefreshSlot(int index)
        {
            ushort itemId =
                hotbar.GetItemId(index);

            RefreshQuantity(index);
            RefreshDurabilityBar(index);

            RawImage image =
                slotImages[index];

            if (itemId == 0)
            {
                ClearImage(image);
                return;
            }

            if (!itemRegistry.TryGetDefinition(
                    itemId,
                    out ItemDefinition itemDefinition))
            {
                SetInvalidImage(image);
                return;
            }

            if (!itemDefinition.RepresentsBlock)
            {
                SetItemIcon(
                    image,
                    itemDefinition.Icon
                );

                return;
            }

            ushort blockId =
                itemDefinition.BlockId;

            if (blockId == BlockIds.Air)
            {
                ClearImage(image);
                return;
            }

            if (!blockRegistry.TryGetDefinition(
                    blockId,
                    out BlockDefinition blockDefinition))
            {
                SetInvalidImage(image);
                return;
            }

            AtlasTileCoordinate tile =
                blockDefinition.SideTexture;

            image.texture =
                atlasTexture;

            image.uvRect =
                GetTileUVRect(tile);

            image.color =
                Color.white;
        }

        private void RefreshQuantities()
        {
            for (int i = 0;
                 i < VoxelPlayerHotbar.SlotCount;
                 i++)
            {
                RefreshQuantity(i);
            }
        }

        private void RefreshQuantity(int index)
        {
            if (quantityTexts == null ||
                quantityTexts[index] == null)
            {
                return;
            }

            ushort itemId =
                hotbar.GetItemId(index);

            ushort quantity =
                hotbar.GetItemQuantity(index);

            if (itemId == 0 ||
                quantity <= 1)
            {
                quantityTexts[index].text =
                    string.Empty;

                return;
            }

            quantityTexts[index].text =
                quantity.ToString();
        }

        private void RefreshDurabilityBars()
        {
            for (int i = 0;
                 i < VoxelPlayerHotbar.SlotCount;
                 i++)
            {
                RefreshDurabilityBar(i);
            }
        }

        private void RefreshDurabilityBar(int index)
        {
            if (durabilityBackgrounds == null ||
                durabilityFills == null ||
                durabilityBackgrounds[index] == null ||
                durabilityFills[index] == null)
            {
                return;
            }

            ushort itemId =
                hotbar.GetItemId(index);

            if (itemId == 0)
            {
                SetDurabilityBarVisible(
                    index,
                    false
                );

                return;
            }

            if (!itemRegistry.TryGetRuntimeData(
                    itemId,
                    out ItemRuntimeData itemData))
            {
                SetDurabilityBarVisible(
                    index,
                    false
                );

                return;
            }

            if (!itemData.HasDurability ||
                itemData.MaxDurability == 0)
            {
                SetDurabilityBarVisible(
                    index,
                    false
                );

                return;
            }

            ushort durability =
                hotbar.GetItemDurability(index);

            float normalized =
                Mathf.Clamp01(
                    (float)durability /
                    itemData.MaxDurability
                );

            SetDurabilityBarVisible(
                index,
                true
            );

            RectTransform fillRect =
                durabilityFills[index]
                    .rectTransform;

            fillRect.anchorMax =
                new Vector2(
                    normalized,
                    1f
                );

            fillRect.sizeDelta =
                Vector2.zero;

            durabilityFills[index].color =
                GetDurabilityColor(normalized);
        }

        private void SetDurabilityBarVisible(
            int index,
            bool visible)
        {
            durabilityBackgrounds[index].gameObject
                .SetActive(visible);

            durabilityFills[index].gameObject
                .SetActive(visible);
        }

        private Color GetDurabilityColor(
            float normalized)
        {
            if (normalized <= 0.25f)
            {
                return durabilityLowColor;
            }

            if (normalized <= 0.5f)
            {
                return durabilityMediumColor;
            }

            return durabilityFullColor;
        }

        private void SetItemIcon(
            RawImage image,
            AtlasTileCoordinate tile)
        {
            image.texture =
                atlasTexture;

            image.uvRect =
                GetTileUVRect(tile);

            image.color =
                Color.white;
        }

        private void ClearImage(
            RawImage image)
        {
            image.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    0f
                );

            image.uvRect =
                new Rect(
                    0f,
                    0f,
                    1f,
                    1f
                );
        }

        private void SetInvalidImage(
            RawImage image)
        {
            image.color =
                new Color(
                    1f,
                    0f,
                    1f,
                    1f
                );

            image.uvRect =
                new Rect(
                    0f,
                    0f,
                    1f,
                    1f
                );
        }

        private void UpdateSelection()
        {
            int selected =
                hotbar.SelectedSlot;

            for (int i = 0;
                 i < VoxelPlayerHotbar.SlotCount;
                 i++)
            {
                Image selection =
                    selectionImages[i];

                if (i == selected)
                {
                    selection.color =
                        Color.white;
                }
                else
                {
                    selection.color =
                        new Color(
                            1f,
                            1f,
                            1f,
                            0f
                        );
                }
            }
        }

        private Rect GetTileUVRect(
            AtlasTileCoordinate tile)
        {
            int columns =
                atlasSettings.Columns;

            int rows =
                atlasSettings.Rows;

            float width =
                1f / columns;

            float height =
                1f / rows;

            float x =
                tile.Column * width;

            float y;

            if (atlasSettings.RowZeroIsTop)
            {
                y =
                    1f -
                    ((tile.Row + 1) * height);
            }
            else
            {
                y =
                    tile.Row * height;
            }

            return new Rect(
                x,
                y,
                width,
                height
            );
        }

        private RectTransform CreateRectTransform(
            string objectName,
            Transform parent)
        {
            GameObject objectInstance =
                new GameObject(
                    objectName,
                    typeof(RectTransform)
                );

            objectInstance.transform.SetParent(
                parent,
                false
            );

            return objectInstance.GetComponent<RectTransform>();
        }

        private Texture2D GetWhiteTexture()
        {
            if (generatedWhiteTexture != null)
            {
                return generatedWhiteTexture;
            }

            generatedWhiteTexture =
                new Texture2D(
                    1,
                    1,
                    TextureFormat.RGBA32,
                    false
                );

            generatedWhiteTexture.name =
                "VoxelHotbarUI_White";

            generatedWhiteTexture.SetPixel(
                0,
                0,
                Color.white
            );

            generatedWhiteTexture.Apply();

            return generatedWhiteTexture;
        }

        private void OnDestroy()
        {
            if (generatedWhiteTexture != null)
            {
                Destroy(generatedWhiteTexture);
                generatedWhiteTexture = null;
            }
        }
    }
}
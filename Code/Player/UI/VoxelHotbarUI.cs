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

        private RectTransform hotbarRoot;
        private RawImage[] slotImages;
        private Image[] selectionImages;

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
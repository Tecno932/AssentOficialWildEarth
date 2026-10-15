
using UnityEngine;
using UnityEngine.UI;

namespace WildEarth.Voxel
{
    public sealed class VoxelTargetWaybarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VoxelPlayerInteraction interaction;
        [SerializeField] private ItemRegistry itemRegistry;
        [SerializeField] private VoxelAtlasSettings atlasSettings;
        [SerializeField] private Texture2D atlasTexture;

        [Header("Layout")]
        [SerializeField, Min(1f)] private float panelWidth = 440f;
        [SerializeField, Min(1f)] private float panelHeight = 92f;
        [SerializeField, Min(1f)] private float iconSize = 64f;
        [SerializeField] private float topOffset = 24f;

        [Header("Colors")]
        [SerializeField] private Color panelColor =
            new Color(0.04f, 0.04f, 0.04f, 0.88f);

        [SerializeField] private Color textColor = Color.white;

        private RectTransform panel;
        private RawImage blockIcon;
        private RawImage toolIcon;
        private Text blockNameText;
        private Text detailsText;
        private Text toolText;

        private Texture2D whiteTexture;
        private bool initialized;
        private ushort lastBlockId;
        private ushort lastToolItemId;
        private int lastRequiredLevel;
        private ToolType lastRequiredTool;
        private string lastBlockName;

        private void Awake()
        {
            if (interaction == null)
                throw new System.InvalidOperationException(
                    "VoxelTargetWaybarUI requiere VoxelPlayerInteraction.");

            if (itemRegistry == null)
                throw new System.InvalidOperationException(
                    "VoxelTargetWaybarUI requiere ItemRegistry.");

            if (atlasSettings == null)
                throw new System.InvalidOperationException(
                    "VoxelTargetWaybarUI requiere VoxelAtlasSettings.");

            if (atlasTexture == null)
                throw new System.InvalidOperationException(
                    "VoxelTargetWaybarUI requiere la textura del atlas.");

            BuildUI();
            initialized = true;
            HidePanel();
        }

        private void Update()
        {
            if (!initialized)
                return;

            if (!interaction.TryGetTargetBlock(
                    out _,
                    out BlockDefinition block))
            {
                HidePanel();
                return;
            }

            panel.gameObject.SetActive(true);

            if (lastBlockId != block.Id ||
                lastBlockName != block.BlockName ||
                lastRequiredTool != block.RequiredTool ||
                lastRequiredLevel != block.RequiredToolLevel)
            {
                RefreshBlock(block);
            }
        }

        private void BuildUI()
        {
            panel = CreateRect(
                "TargetWaybar",
                transform);

            panel.anchorMin = new Vector2(0.5f, 1f);
            panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, -topOffset);
            panel.sizeDelta = new Vector2(panelWidth, panelHeight);

            RawImage background =
                panel.gameObject.AddComponent<RawImage>();

            background.texture = GetWhiteTexture();
            background.color = panelColor;
            background.raycastTarget = false;

            blockIcon = CreateIcon(
                "BlockIcon",
                panel,
                new Vector2(12f, -14f),
                iconSize);

            blockNameText = CreateText(
                "BlockName",
                panel,
                new Vector2(88f, -12f),
                new Vector2(panelWidth - 190f, 30f),
                21,
                FontStyle.Bold);

            detailsText = CreateText(
                "BlockDetails",
                panel,
                new Vector2(88f, -43f),
                new Vector2(panelWidth - 190f, 36f),
                14,
                FontStyle.Normal);

            toolIcon = CreateIcon(
                "RequiredToolIcon",
                panel,
                new Vector2(panelWidth - 76f, -14f),
                52f);

            toolText = CreateText(
                "RequiredTool",
                panel,
                new Vector2(panelWidth - 82f, -68f),
                new Vector2(76f, 18f),
                11,
                FontStyle.Bold);

            toolText.alignment = TextAnchor.MiddleCenter;
        }

        private void RefreshBlock(BlockDefinition block)
        {
            blockNameText.text = block.BlockName;

            string hardness =
                block.Hardness.ToString("0.##");

            if (block.RequiredTool == ToolType.None)
            {
                detailsText.text =
                    $"Dureza: {hardness}\nHerramienta: ninguna";

                toolText.text = string.Empty;
                toolIcon.color = new Color(1f, 1f, 1f, 0f);
                lastToolItemId = 0;
            }
            else
            {
                detailsText.text =
                    $"Dureza: {hardness}\n" +
                    $"Requiere: {block.RequiredTool} " +
                    $"(nivel {block.RequiredToolLevel})";

                toolText.text = block.RequiredTool.ToString();

                RefreshToolIcon(
                    block.RequiredTool,
                    block.RequiredToolLevel);
            }

            SetTile(blockIcon, block.SideTexture);

            lastBlockId = block.Id;
            lastBlockName = block.BlockName;
            lastRequiredTool = block.RequiredTool;
            lastRequiredLevel = block.RequiredToolLevel;
        }

        private void RefreshToolIcon(
            ToolType requiredTool,
            byte requiredLevel)
        {
            ItemDefinition bestItem = null;
            int bestLevel = int.MaxValue;

            foreach (ItemDefinition item in itemRegistry.Definitions)
            {
                if (item == null ||
                    item.ItemType != ItemType.Tool ||
                    item.ToolType != requiredTool ||
                    item.ToolLevel < requiredLevel)
                {
                    continue;
                }

                if (item.ToolLevel < bestLevel)
                {
                    bestItem = item;
                    bestLevel = item.ToolLevel;
                }
            }

            if (bestItem == null)
            {
                toolIcon.color = new Color(1f, 1f, 1f, 0f);
                lastToolItemId = 0;
                return;
            }

            lastToolItemId = bestItem.Id;
            SetTile(toolIcon, bestItem.Icon);
        }

        private void SetTile(
            RawImage image,
            AtlasTileCoordinate tile)
        {
            image.texture = atlasTexture;

            float width = 1f / atlasSettings.Columns;
            float height = 1f / atlasSettings.Rows;

            float x = tile.Column * width;

            float y = atlasSettings.RowZeroIsTop
                ? 1f - ((tile.Row + 1) * height)
                : tile.Row * height;

            image.uvRect = new Rect(x, y, width, height);
            image.color = Color.white;
        }

        private RawImage CreateIcon(
            string objectName,
            RectTransform parent,
            Vector2 position,
            float size)
        {
            RectTransform rect = CreateRect(objectName, parent);

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);

            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = atlasTexture;
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = false;

            return image;
        }

        private Text CreateText(
            string objectName,
            RectTransform parent,
            Vector2 position,
            Vector2 size,
            int fontSize,
            FontStyle style)
        {
            RectTransform rect = CreateRect(objectName, parent);

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = textColor;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            return text;
        }

        private RectTransform CreateRect(
            string objectName,
            Transform parent)
        {
            GameObject instance = new GameObject(
                objectName,
                typeof(RectTransform));

            instance.transform.SetParent(parent, false);

            return instance.GetComponent<RectTransform>();
        }

        private Texture2D GetWhiteTexture()
        {
            if (whiteTexture != null)
                return whiteTexture;

            whiteTexture = new Texture2D(
                1, 1, TextureFormat.RGBA32, false);

            whiteTexture.name = "VoxelTargetWaybar_White";
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();

            return whiteTexture;
        }

        private void HidePanel()
        {
            if (panel != null)
                panel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (whiteTexture != null)
                Destroy(whiteTexture);
        }
    }
}

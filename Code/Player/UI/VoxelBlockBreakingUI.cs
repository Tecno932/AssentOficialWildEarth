using UnityEngine;
using UnityEngine.UI;

namespace WildEarth.Voxel
{
    public sealed class VoxelBlockBreakingUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VoxelPlayerInteraction interaction;
        [SerializeField] private VoxelAtlasSettings atlasSettings;
        [SerializeField] private Texture2D atlasTexture;

        [Header("Breaking Overlay")]
        [SerializeField, Min(1f)] private float overlaySize = 1f;
        [SerializeField] private Color overlayColor =
            new Color(1f, 1f, 1f, 0.9f);

        [Header("Atlas")]
        [SerializeField, Min(1)] private int stageCount = 10;

        /*
         * Las etapas se configuran después de crear las texturas.
         *
         * Cada elemento corresponde a una etapa:
         *
         * 0 = rotura inicial
         * 1 = rotura 2
         * 2 = rotura 3
         * ...
         * 9 = rotura final
         *
         * Column/Row utilizan las coordenadas del atlas.
         */
        [SerializeField]
        private AtlasTileCoordinate[] breakStages;

        private RawImage overlayImage;
        private RectTransform overlayRect;

        private bool initialized;

        private void Awake()
        {
            ValidateReferences();
            BuildOverlay();

            Hide();

            initialized = true;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            if (!interaction.IsBreaking)
            {
                Hide();
                return;
            }

            float progress =
                interaction.BreakingProgress;

            UpdateStage(progress);
        }

        private void ValidateReferences()
        {
            if (interaction == null)
            {
                throw new System.InvalidOperationException(
                    "VoxelBlockBreakingUI requiere VoxelPlayerInteraction."
                );
            }

            if (atlasSettings == null)
            {
                throw new System.InvalidOperationException(
                    "VoxelBlockBreakingUI requiere VoxelAtlasSettings."
                );
            }

            if (atlasTexture == null)
            {
                throw new System.InvalidOperationException(
                    "VoxelBlockBreakingUI requiere la textura del atlas."
                );
            }

            if (breakStages == null ||
                breakStages.Length == 0)
            {
                throw new System.InvalidOperationException(
                    "VoxelBlockBreakingUI requiere al menos una etapa de rotura."
                );
            }
        }

        private void BuildOverlay()
        {
            GameObject objectInstance =
                new GameObject(
                    "BlockBreakingOverlay",
                    typeof(RectTransform),
                    typeof(RawImage)
                );

            objectInstance.transform.SetParent(
                transform,
                false
            );

            overlayRect =
                objectInstance.GetComponent<RectTransform>();

            overlayRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            overlayRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            overlayRect.pivot =
                new Vector2(0.5f, 0.5f);

            overlayRect.sizeDelta =
                new Vector2(
                    overlaySize,
                    overlaySize
                );

            overlayRect.anchoredPosition =
                Vector2.zero;

            overlayImage =
                objectInstance.GetComponent<RawImage>();

            overlayImage.texture =
                atlasTexture;

            overlayImage.color =
                overlayColor;

            overlayImage.raycastTarget =
                false;
        }

        private void UpdateStage(float progress)
        {
            int stage =
                Mathf.FloorToInt(
                    progress *
                    breakStages.Length
                );

            stage =
                Mathf.Clamp(
                    stage,
                    0,
                    breakStages.Length - 1
                );

            overlayImage.uvRect =
                GetTileUVRect(
                    breakStages[stage]
                );

            overlayImage.enabled = true;
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

        private void Hide()
        {
            if (overlayImage != null)
            {
                overlayImage.enabled = false;
            }
        }
    }
}
using System;
using UnityEngine;

namespace WildEarth.Voxel
{
    [CreateAssetMenu(
        fileName = "VoxelAtlasSettings",
        menuName = "WildEarth/Voxel/Atlas Settings"
    )]
    public sealed class VoxelAtlasSettings : ScriptableObject
    {
        [Header("Atlas")]
        [SerializeField]
        [Min(1)]
        private int atlasWidth = 512;

        [SerializeField]
        [Min(1)]
        private int atlasHeight = 512;

        [Header("Cell")]
        [SerializeField]
        [Min(1)]
        private int cellWidth = 16;

        [SerializeField]
        [Min(1)]
        private int cellHeight = 16;

        public int AtlasWidth => atlasWidth;

        public int AtlasHeight => atlasHeight;

        public int CellWidth => cellWidth;

        public int CellHeight => cellHeight;

        public int Columns
        {
            get
            {
                ValidateConfiguration();

                return atlasWidth / cellWidth;
            }
        }

        public int Rows
        {
            get
            {
                ValidateConfiguration();

                return atlasHeight / cellHeight;
            }
        }

        public int TileCount
        {
            get
            {
                return Columns * Rows;
            }
        }

        public Vector2 GetTileSizeUV()
        {
            ValidateConfiguration();

            return new Vector2(
                (float)cellWidth / atlasWidth,
                (float)cellHeight / atlasHeight
            );
        }

        public Vector2 GetTileMinUV(
            int textureIndex)
        {
            ValidateTextureIndex(textureIndex);

            int tileX =
                textureIndex % Columns;

            int tileY =
                textureIndex / Columns;

            Vector2 tileSize =
                GetTileSizeUV();

            return new Vector2(
                tileX * tileSize.x,
                tileY * tileSize.y
            );
        }

        public Vector2 GetTileMaxUV(
            int textureIndex)
        {
            Vector2 min =
                GetTileMinUV(textureIndex);

            Vector2 size =
                GetTileSizeUV();

            return min + size;
        }

        public Vector2 GetUV(
            int textureIndex,
            int corner)
        {
            Vector2 min =
                GetTileMinUV(textureIndex);

            Vector2 max =
                GetTileMaxUV(textureIndex);

            switch (corner)
            {
                case 0:
                    return new Vector2(
                        min.x,
                        min.y
                    );

                case 1:
                    return new Vector2(
                        max.x,
                        min.y
                    );

                case 2:
                    return new Vector2(
                        max.x,
                        max.y
                    );

                case 3:
                    return new Vector2(
                        min.x,
                        max.y
                    );

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(corner),
                        corner,
                        "Esquina UV inválida. Debe estar entre 0 y 3."
                    );
            }
        }

        private void ValidateTextureIndex(
            int textureIndex)
        {
            ValidateConfiguration();

            if (textureIndex < 0 ||
                textureIndex >= TileCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(textureIndex),
                    textureIndex,
                    $"Índice de textura fuera del atlas. " +
                    $"Rango válido: 0-{TileCount - 1}."
                );
            }
        }

        private void ValidateConfiguration()
        {
            if (atlasWidth <= 0)
            {
                throw new InvalidOperationException(
                    "VoxelAtlasSettings: Atlas Width debe ser mayor que 0."
                );
            }

            if (atlasHeight <= 0)
            {
                throw new InvalidOperationException(
                    "VoxelAtlasSettings: Atlas Height debe ser mayor que 0."
                );
            }

            if (cellWidth <= 0)
            {
                throw new InvalidOperationException(
                    "VoxelAtlasSettings: Cell Width debe ser mayor que 0."
                );
            }

            if (cellHeight <= 0)
            {
                throw new InvalidOperationException(
                    "VoxelAtlasSettings: Cell Height debe ser mayor que 0."
                );
            }

            if (atlasWidth % cellWidth != 0)
            {
                throw new InvalidOperationException(
                    "VoxelAtlasSettings: Atlas Width debe ser " +
                    "divisible exactamente por Cell Width."
                );
            }

            if (atlasHeight % cellHeight != 0)
            {
                throw new InvalidOperationException(
                    "VoxelAtlasSettings: Atlas Height debe ser " +
                    "divisible exactamente por Cell Height."
                );
            }
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            if (atlasWidth < 1)
                atlasWidth = 1;

            if (atlasHeight < 1)
                atlasHeight = 1;

            if (cellWidth < 1)
                cellWidth = 1;

            if (cellHeight < 1)
                cellHeight = 1;
        }

#endif
    }
}
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

        [Header("Orientation")]
        [SerializeField]
        private bool rowZeroIsTop = true;

        public int AtlasWidth => atlasWidth;

        public int AtlasHeight => atlasHeight;

        public int CellWidth => cellWidth;

        public int CellHeight => cellHeight;

        public bool RowZeroIsTop => rowZeroIsTop;

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
            AtlasTileCoordinate tile)
        {
            ValidateTile(tile);

            int tileX = tile.Column;

            int tileY = rowZeroIsTop
                ? Rows - 1 - tile.Row
                : tile.Row;

            Vector2 tileSize =
                GetTileSizeUV();

            return new Vector2(
                tileX * tileSize.x,
                tileY * tileSize.y
            );
        }

        public Vector2 GetTileMaxUV(
            AtlasTileCoordinate tile)
        {
            Vector2 min =
                GetTileMinUV(tile);

            Vector2 size =
                GetTileSizeUV();

            return min + size;
        }

        public Vector2 GetUV(
            AtlasTileCoordinate tile,
            int corner)
        {
            Vector2 min =
                GetTileMinUV(tile);

            Vector2 max =
                GetTileMaxUV(tile);

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

        private void ValidateTile(
            AtlasTileCoordinate tile)
        {
            ValidateConfiguration();

            if (tile.Column < 0 ||
                tile.Column >= Columns)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tile.Column),
                    tile.Column,
                    $"Columna fuera del atlas. Rango: 0-{Columns - 1}."
                );
            }

            if (tile.Row < 0 ||
                tile.Row >= Rows)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tile.Row),
                    tile.Row,
                    $"Fila fuera del atlas. Rango: 0-{Rows - 1}."
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
                    "VoxelAtlasSettings: Atlas Width debe ser divisible " +
                    "exactamente por Cell Width."
                );
            }

            if (atlasHeight % cellHeight != 0)
            {
                throw new InvalidOperationException(
                    "VoxelAtlasSettings: Atlas Height debe ser divisible " +
                    "exactamente por Cell Height."
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
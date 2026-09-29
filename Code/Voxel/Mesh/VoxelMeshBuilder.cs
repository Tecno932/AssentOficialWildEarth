using System;
using UnityEngine;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    public sealed class VoxelMeshBuilder
    {
        private readonly BlockRuntimeDatabase blockDatabase;
        private readonly VoxelAtlasSettings atlasSettings;

        private const int BinaryMaskSize =
            VoxelConstants.ChunkSize;

        private const int MaskCellCount =
            BinaryMaskSize *
            BinaryMaskSize;

        private const ushort GrassBlockId = 3;

        public VoxelMeshBuilder(
            BlockRuntimeDatabase blockDatabase,
            VoxelAtlasSettings atlasSettings)
        {
            this.blockDatabase =
                blockDatabase ??
                throw new ArgumentNullException(
                    nameof(blockDatabase)
                );

            this.atlasSettings =
                atlasSettings ??
                throw new ArgumentNullException(
                    nameof(atlasSettings)
                );
        }

        public ChunkMeshData Build(
            Chunk chunk,
            ChunkStorage storage)
        {
            if (chunk == null)
                throw new ArgumentNullException(
                    nameof(chunk)
                );

            if (storage == null)
                throw new ArgumentNullException(
                    nameof(storage)
                );

            if (!chunk.Data.IsCreated)
            {
                throw new InvalidOperationException(
                    $"El chunk {chunk.Coordinate} no tiene datos válidos."
                );
            }

            int chunkSize =
                VoxelConstants.ChunkSize;

            ChunkMeshData mesh =
                new ChunkMeshData(
                    chunkSize *
                    chunkSize *
                    6
                );

            ushort[] voxelIds =
                BuildVoxelCache(
                    chunk,
                    storage
                );

            for (
                int face = 0;
                face < 6;
                face++)
            {
                BuildBinaryGreedyDirection(
                    mesh,
                    voxelIds,
                    chunk,
                    (VoxelFace)face
                );
            }

            return mesh;
        }

        private ushort[] BuildVoxelCache(
            Chunk chunk,
            ChunkStorage storage)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            int size =
                chunkSize + 2;

            ushort[] cache =
                new ushort[
                    size *
                    size *
                    size
                ];

            for (
                int y = 0;
                y < chunkSize;
                y++)
            {
                for (
                    int z = 0;
                    z < chunkSize;
                    z++)
                {
                    for (
                        int x = 0;
                        x < chunkSize;
                        x++)
                    {
                        Voxel voxel =
                            ChunkDataAccess.GetVoxel(
                                chunk.Data,
                                x,
                                y,
                                z
                            );

                        cache[
                            CacheIndex(
                                x + 1,
                                y + 1,
                                z + 1,
                                size
                            )
                        ] =
                            voxel.BlockId;
                    }
                }
            }

            for (
                int i = 0;
                i < chunkSize;
                i++)
            {
                for (
                    int j = 0;
                    j < chunkSize;
                    j++)
                {
                    cache[
                        CacheIndex(
                            0,
                            j + 1,
                            i + 1,
                            size
                        )
                    ] =
                        GetNeighborBlockId(
                            storage,
                            chunk,
                            -1,
                            j,
                            i
                        );

                    cache[
                        CacheIndex(
                            chunkSize + 1,
                            j + 1,
                            i + 1,
                            size
                        )
                    ] =
                        GetNeighborBlockId(
                            storage,
                            chunk,
                            chunkSize,
                            j,
                            i
                        );

                    cache[
                        CacheIndex(
                            i + 1,
                            0,
                            j + 1,
                            size
                        )
                    ] =
                        GetNeighborBlockId(
                            storage,
                            chunk,
                            i,
                            -1,
                            j
                        );

                    cache[
                        CacheIndex(
                            i + 1,
                            chunkSize + 1,
                            j + 1,
                            size
                        )
                    ] =
                        GetNeighborBlockId(
                            storage,
                            chunk,
                            i,
                            chunkSize,
                            j
                        );

                    cache[
                        CacheIndex(
                            i + 1,
                            j + 1,
                            0,
                            size
                        )
                    ] =
                        GetNeighborBlockId(
                            storage,
                            chunk,
                            i,
                            j,
                            -1
                        );

                    cache[
                        CacheIndex(
                            i + 1,
                            j + 1,
                            chunkSize + 1,
                            size
                        )
                    ] =
                        GetNeighborBlockId(
                            storage,
                            chunk,
                            i,
                            j,
                            chunkSize
                        );
                }
            }

            return cache;
        }

        private ushort GetNeighborBlockId(
            ChunkStorage storage,
            Chunk chunk,
            int x,
            int y,
            int z)
        {
            bool resolved =
                ChunkNeighborAccess.TryGetVoxel(
                    storage,
                    chunk.Coordinate,
                    x,
                    y,
                    z,
                    out Voxel voxel
                );

            return resolved
                ? voxel.BlockId
                : BlockIds.Air;
        }

        private void BuildBinaryGreedyDirection(
            ChunkMeshData mesh,
            ushort[] voxelIds,
            Chunk chunk,
            VoxelFace face)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            int cacheSize =
                chunkSize + 2;

            ushort[] binaryRows =
                new ushort[
                    BinaryMaskSize
                ];

            int[] keys =
                new int[
                    MaskCellCount
                ];

            ushort[] blockIds =
                new ushort[
                    MaskCellCount
                ];

            byte[] biomeIds =
                new byte[
                    MaskCellCount
                ];

            for (
                int slice = 0;
                slice < chunkSize;
                slice++)
            {
                Array.Clear(
                    binaryRows,
                    0,
                    binaryRows.Length
                );

                Array.Clear(
                    keys,
                    0,
                    keys.Length
                );

                Array.Clear(
                    blockIds,
                    0,
                    blockIds.Length
                );

                Array.Clear(
                    biomeIds,
                    0,
                    biomeIds.Length
                );

                BuildBinaryFaceMask(
                    binaryRows,
                    keys,
                    blockIds,
                    biomeIds,
                    voxelIds,
                    chunk,
                    face,
                    slice,
                    cacheSize
                );

                BinaryGreedyMerge(
                    mesh,
                    binaryRows,
                    keys,
                    blockIds,
                    biomeIds,
                    face,
                    slice
                );
            }
        }

        private void BuildBinaryFaceMask(
            ushort[] binaryRows,
            int[] keys,
            ushort[] blockIds,
            byte[] biomeIds,
            ushort[] voxelIds,
            Chunk chunk,
            VoxelFace face,
            int slice,
            int cacheSize)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            for (
                int v = 0;
                v < chunkSize;
                v++)
            {
                ushort rowMask = 0;

                for (
                    int u = 0;
                    u < chunkSize;
                    u++)
                {
                    GetVoxelCoordinate(
                        face,
                        slice,
                        u,
                        v,
                        out int x,
                        out int y,
                        out int z
                    );

                    ushort blockId =
                        GetCachedBlockId(
                            voxelIds,
                            x,
                            y,
                            z,
                            cacheSize
                        );

                    if (blockId == BlockIds.Air)
                        continue;

                    if (!blockDatabase.TryGet(
                            blockId,
                            out BlockRuntimeData block))
                    {
                        throw new InvalidOperationException(
                            $"VoxelMeshBuilder encontró un BlockId inválido. " +
                            $"BlockId={blockId}."
                        );
                    }

                    if (block.MeshType != BlockMeshType.Cube)
                        continue;

                    int neighborX = x;
                    int neighborY = y;
                    int neighborZ = z;

                    switch (face)
                    {
                        case VoxelFace.Bottom:
                            neighborY--;
                            break;

                        case VoxelFace.Top:
                            neighborY++;
                            break;

                        case VoxelFace.North:
                            neighborZ++;
                            break;

                        case VoxelFace.South:
                            neighborZ--;
                            break;

                        case VoxelFace.East:
                            neighborX++;
                            break;

                        case VoxelFace.West:
                            neighborX--;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(
                                nameof(face)
                            );
                    }

                    ushort neighborId =
                        GetCachedBlockId(
                            voxelIds,
                            neighborX,
                            neighborY,
                            neighborZ,
                            cacheSize
                        );

                    if (neighborId != BlockIds.Air)
                    {
                        if (blockDatabase.TryGet(
                                neighborId,
                                out BlockRuntimeData neighborBlock) &&
                            neighborBlock.OccludesFaces)
                        {
                            continue;
                        }
                    }

                    int biomeIndex =
                        x +
                        z * chunkSize;

                    BiomeId biomeId =
                        chunk.BiomeData.Biomes[
                            biomeIndex
                        ];

                    int textureKey =
                        GetTextureKey(
                            block,
                            face
                        );

                    int mergeKey =
                        GetMergeKey(
                            textureKey,
                            blockId,
                            biomeId
                        );

                    int index =
                        u +
                        v * chunkSize;

                    keys[index] =
                        mergeKey;

                    blockIds[index] =
                        blockId;

                    biomeIds[index] =
                        (byte)biomeId;

                    rowMask |=
                        (ushort)(1 << u);
                }

                binaryRows[v] =
                    rowMask;
            }
        }

private void BinaryGreedyMerge(
    ChunkMeshData mesh,
    ushort[] binaryRows,
    int[] keys,
    ushort[] blockIds,
    byte[] biomeIds,
    VoxelFace face,
    int slice)
{
    int chunkSize =
        VoxelConstants.ChunkSize;

    for (
        int v = 0;
        v < chunkSize;
        v++)
    {
        while (binaryRows[v] != 0)
        {
            int u =
                FindFirstSetBit(
                    binaryRows[v]
                );

            int index =
                u +
                v * chunkSize;

            int key =
                keys[index];

            if (key == 0)
            {
                binaryRows[v] &=
                    (ushort)~(1 << u);

                continue;
            }

            int width =
                FindBinaryWidth(
                    binaryRows[v],
                    keys,
                    v,
                    u,
                    key
                );

            int height =
                FindBinaryHeight(
                    binaryRows,
                    keys,
                    v,
                    u,
                    width,
                    key
                );

            // Guardar estos datos ANTES de limpiar el rectángulo.
            ushort blockId =
                blockIds[index];

            BiomeId biomeId =
                (BiomeId)biomeIds[index];

            ushort rectangleMask =
                CreateRectangleMask(
                    u,
                    width
                );

            ClearBinaryRectangle(
                binaryRows,
                keys,
                blockIds,
                biomeIds,
                v,
                u,
                width,
                height,
                rectangleMask
            );

            if (!blockDatabase.TryGet(
                    blockId,
                    out BlockRuntimeData block))
            {
                throw new InvalidOperationException(
                    $"VoxelMeshBuilder no pudo resolver " +
                    $"BlockId={blockId}."
                );
            }

            AtlasTileCoordinate texture =
                GetTexture(
                    block,
                    face
                );

            Color32 tint =
                GetBlockTint(
                    blockId,
                    biomeId
                );

            AddGreedyFace(
                mesh,
                face,
                slice,
                u,
                v,
                width,
                height,
                texture,
                tint
            );
        }
    }
}

        private int FindFirstSetBit(
            ushort value)
        {
            for (
                int bit = 0;
                bit < BinaryMaskSize;
                bit++)
            {
                if ((value & (1 << bit)) != 0)
                    return bit;
            }

            return 0;
        }

        private int FindBinaryWidth(
            ushort row,
            int[] keys,
            int v,
            int startU,
            int key)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            int width = 0;

            for (
                int u = startU;
                u < chunkSize;
                u++)
            {
                if ((row & (1 << u)) == 0)
                    break;

                if (keys[
                        u +
                        v * chunkSize
                    ] != key)
                {
                    break;
                }

                width++;
            }

            return width;
        }

        private int FindBinaryHeight(
            ushort[] binaryRows,
            int[] keys,
            int startV,
            int startU,
            int width,
            int key)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            int height = 1;

            ushort rectangleMask =
                CreateRectangleMask(
                    startU,
                    width
                );

            while (
                startV + height <
                chunkSize)
            {
                ushort nextRow =
                    binaryRows[
                        startV + height
                    ];

                if ((nextRow & rectangleMask) !=
                    rectangleMask)
                {
                    break;
                }

                bool sameKey = true;

                for (
                    int u = startU;
                    u < startU + width;
                    u++)
                {
                    if (keys[
                            u +
                            (startV + height) *
                            chunkSize
                        ] != key)
                    {
                        sameKey = false;
                        break;
                    }
                }

                if (!sameKey)
                    break;

                height++;
            }

            return height;
        }

        private ushort CreateRectangleMask(
            int startU,
            int width)
        {
            int mask =
                ((1 << width) - 1)
                << startU;

            return (ushort)mask;
        }

        private void ClearBinaryRectangle(
            ushort[] binaryRows,
            int[] keys,
            ushort[] blockIds,
            byte[] biomeIds,
            int startV,
            int startU,
            int width,
            int height,
            ushort rectangleMask)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            for (
                int y = 0;
                y < height;
                y++)
            {
                int row =
                    startV + y;

                binaryRows[row] &=
                    (ushort)~rectangleMask;

                for (
                    int x = 0;
                    x < width;
                    x++)
                {
                    int index =
                        startU +
                        x +
                        row *
                        chunkSize;

                    keys[index] = 0;
                    blockIds[index] = 0;
                    biomeIds[index] = 0;
                }
            }
        }

        private void AddGreedyFace(
            ChunkMeshData mesh,
            VoxelFace face,
            int slice,
            int u,
            int v,
            int width,
            int height,
            AtlasTileCoordinate texture,
            Color32 tint)
        {
            float2 atlasTileMin =
                GetAtlasTileMin(
                    texture
                );

            float2 uv0 =
                new float2(
                    0f,
                    0f
                );

            float2 uv1 =
                new float2(
                    width,
                    0f
                );

            float2 uv2 =
                new float2(
                    width,
                    height
                );

            float2 uv3 =
                new float2(
                    0f,
                    height
                );

            float x0;
            float x1;
            float y0;
            float y1;
            float z0;
            float z1;

            switch (face)
            {
                case VoxelFace.Bottom:
                    x0 = u;
                    x1 = u + width;
                    y0 = slice;
                    z0 = v;
                    z1 = v + height;

                    mesh.AddTiledQuad(
                        new float3(x0, y0, z0),
                        new float3(x1, y0, z0),
                        new float3(x1, y0, z1),
                        new float3(x0, y0, z1),
                        atlasTileMin,
                        uv0,
                        uv1,
                        uv2,
                        uv3,
                        tint
                    );
                    break;

                case VoxelFace.Top:
                    x0 = u;
                    x1 = u + width;
                    y0 = slice + 1;
                    z0 = v;
                    z1 = v + height;

                    mesh.AddTiledQuad(
                        new float3(x0, y0, z1),
                        new float3(x1, y0, z1),
                        new float3(x1, y0, z0),
                        new float3(x0, y0, z0),
                        atlasTileMin,
                        uv0,
                        uv1,
                        uv2,
                        uv3,
                        tint
                    );
                    break;

                case VoxelFace.North:
                    x0 = u;
                    x1 = u + width;
                    y0 = v;
                    y1 = v + height;
                    z0 = slice + 1;

                    mesh.AddTiledQuad(
                        new float3(x0, y0, z0),
                        new float3(x1, y0, z0),
                        new float3(x1, y1, z0),
                        new float3(x0, y1, z0),
                        atlasTileMin,
                        uv0,
                        uv1,
                        uv2,
                        uv3,
                        tint
                    );
                    break;

                case VoxelFace.South:
                    x0 = u;
                    x1 = u + width;
                    y0 = v;
                    y1 = v + height;
                    z0 = slice;

                    mesh.AddTiledQuad(
                        new float3(x1, y0, z0),
                        new float3(x0, y0, z0),
                        new float3(x0, y1, z0),
                        new float3(x1, y1, z0),
                        atlasTileMin,
                        uv0,
                        uv1,
                        uv2,
                        uv3,
                        tint
                    );
                    break;

                case VoxelFace.East:
                    x0 = slice + 1;
                    y0 = v;
                    y1 = v + height;
                    z0 = u;
                    z1 = u + width;

                    mesh.AddTiledQuad(
                        new float3(x0, y0, z1),
                        new float3(x0, y0, z0),
                        new float3(x0, y1, z0),
                        new float3(x0, y1, z1),
                        atlasTileMin,
                        uv0,
                        uv1,
                        uv2,
                        uv3,
                        tint
                    );
                    break;

                case VoxelFace.West:
                    x0 = slice;
                    y0 = v;
                    y1 = v + height;
                    z0 = u;
                    z1 = u + width;

                    mesh.AddTiledQuad(
                        new float3(x0, y0, z0),
                        new float3(x0, y0, z1),
                        new float3(x0, y1, z1),
                        new float3(x0, y1, z0),
                        atlasTileMin,
                        uv0,
                        uv1,
                        uv2,
                        uv3,
                        tint
                    );
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(face)
                    );
            }
        }

        private ushort GetCachedBlockId(
            ushort[] cache,
            int x,
            int y,
            int z,
            int cacheSize)
        {
            return cache[
                CacheIndex(
                    x + 1,
                    y + 1,
                    z + 1,
                    cacheSize
                )
            ];
        }

        private int CacheIndex(
            int x,
            int y,
            int z,
            int size)
        {
            return x +
                   z * size +
                   y * size * size;
        }

        private void GetVoxelCoordinate(
            VoxelFace face,
            int slice,
            int u,
            int v,
            out int x,
            out int y,
            out int z)
        {
            switch (face)
            {
                case VoxelFace.Bottom:
                case VoxelFace.Top:
                    x = u;
                    y = slice;
                    z = v;
                    break;

                case VoxelFace.North:
                case VoxelFace.South:
                    x = u;
                    y = v;
                    z = slice;
                    break;

                case VoxelFace.East:
                case VoxelFace.West:
                    x = slice;
                    y = v;
                    z = u;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(face)
                    );
            }
        }

        private int GetTextureKey(
            BlockRuntimeData block,
            VoxelFace face)
        {
            AtlasTileCoordinate texture =
                GetTexture(
                    block,
                    face
                );

            return
                (block.Id << 16) |
                ((texture.Column & 0xFF) << 8) |
                (texture.Row & 0xFF);
        }

        private int GetMergeKey(
            int textureKey,
            ushort blockId,
            BiomeId biomeId)
        {
            if (blockId != GrassBlockId)
                return textureKey;

            return textureKey ^
                   ((int)biomeId << 24);
        }

        private Color32 GetBlockTint(
            ushort blockId,
            BiomeId biomeId)
        {
            if (blockId != GrassBlockId)
                return Color.white;

            switch (biomeId)
            {
                case BiomeId.Plains:
                    return new Color32(
                        126,
                        181,
                        72,
                        255
                    );

                case BiomeId.Forest:
                    return new Color32(
                        78,
                        145,
                        54,
                        255
                    );

                case BiomeId.Desert:
                    return new Color32(
                        177,
                        171,
                        82,
                        255
                    );

                case BiomeId.Tundra:
                    return new Color32(
                        145,
                        176,
                        113,
                        255
                    );

                case BiomeId.Mountains:
                    return new Color32(
                        102,
                        156,
                        76,
                        255
                    );

                default:
                    return Color.white;
            }
        }

        private AtlasTileCoordinate GetTexture(
            BlockRuntimeData block,
            VoxelFace face)
        {
            switch (face)
            {
                case VoxelFace.Bottom:
                    return block.BottomTexture;

                case VoxelFace.Top:
                    return block.TopTexture;

                case VoxelFace.North:
                case VoxelFace.South:
                case VoxelFace.East:
                case VoxelFace.West:
                    return block.SideTexture;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(face)
                    );
            }
        }

        private float2 GetAtlasTileMin(
            AtlasTileCoordinate texture)
        {
            Vector2 min =
                atlasSettings.GetTileMinUV(
                    texture
                );

            return new float2(
                min.x,
                min.y
            );
        }
    }
}
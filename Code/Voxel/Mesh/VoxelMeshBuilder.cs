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
            VoxelFace face)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            int cacheSize =
                chunkSize + 2;

            // Cada fila representa 16 celdas mediante bits.
            ushort[] binaryRows =
                new ushort[
                    BinaryMaskSize
                ];

            // Mantiene el texture/block key de cada celda.
            int[] keys =
                new int[
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

                BuildBinaryFaceMask(
                    binaryRows,
                    keys,
                    voxelIds,
                    face,
                    slice,
                    cacheSize
                );

                BinaryGreedyMerge(
                    mesh,
                    binaryRows,
                    keys,
                    face,
                    slice
                );
            }
        }

        private void BuildBinaryFaceMask(
            ushort[] binaryRows,
            int[] keys,
            ushort[] voxelIds,
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

                    int textureKey =
                        GetTextureKey(
                            block,
                            face
                        );

                    int index =
                        u +
                        v * chunkSize;

                    keys[index] =
                        textureKey;

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

                    ushort rectangleMask =
                        CreateRectangleMask(
                            u,
                            width
                        );

                    ClearBinaryRectangle(
                        binaryRows,
                        keys,
                        v,
                        u,
                        width,
                        height,
                        rectangleMask
                    );

                    ushort blockId =
                        GetBlockIdFromTextureKey(
                            key
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

                    AddGreedyFace(
                        mesh,
                        face,
                        slice,
                        u,
                        v,
                        width,
                        height,
                        texture
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
                    keys[
                        startU +
                        x +
                        row *
                        chunkSize
                    ] = 0;
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
            AtlasTileCoordinate texture)
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
                        uv3
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
                        uv3
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
                        uv3
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
                        uv3
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
                        uv3
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
                        uv3
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

        private ushort GetBlockIdFromTextureKey(
            int key)
        {
            return (ushort)(key >> 16);
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
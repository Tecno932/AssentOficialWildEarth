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

            bool containsFluid;

            Voxel[] voxelCache =
                BuildVoxelCache(
                    chunk,
                    storage,
                    out containsFluid
                );

            for (
                int face = 0;
                face < 6;
                face++)
            {
                BuildBinaryGreedyDirection(
                    mesh,
                    voxelCache,
                    chunk,
                    (VoxelFace)face
                );
            }

            if (containsFluid)
            {
                BuildFluidGeometry(
                    mesh,
                    chunk,
                    storage,
                    voxelCache
                );
            }

            return mesh;
        }

        private Voxel[] BuildVoxelCache(
            Chunk chunk,
            ChunkStorage storage,
            out bool containsFluid)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            int cacheSize =
                chunkSize + 2;

            Voxel[] cache =
                new Voxel[
                    cacheSize *
                    cacheSize *
                    cacheSize
                ];

            containsFluid = false;

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
                            chunk.Data.Voxels[
                                VoxelIndex.ToIndex(
                                    x,
                                    y,
                                    z
                                )
                            ];

                        cache[
                            CacheIndex(
                                x + 1,
                                y + 1,
                                z + 1,
                                cacheSize
                            )
                        ] = voxel;

                        if (voxel.BlockId != BlockIds.Air &&
                            blockDatabase.TryGet(
                                voxel.BlockId,
                                out BlockRuntimeData block))
                        {
                            if (block.MeshType ==
                                    BlockMeshType.Fluid &&
                                block.IsFluid &&
                                voxel.State > 0)
                            {
                                containsFluid = true;
                            }
                        }
                    }
                }
            }

            // Borde X-
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    Voxel voxel;

                    if (ChunkNeighborAccess.TryGetVoxel(
                            storage,
                            chunk.Coordinate,
                            -1,
                            y,
                            z,
                            out voxel))
                    {
                        cache[
                            CacheIndex(
                                0,
                                y + 1,
                                z + 1,
                                cacheSize
                            )
                        ] = voxel;
                    }
                }
            }

            // Borde X+
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    Voxel voxel;

                    if (ChunkNeighborAccess.TryGetVoxel(
                            storage,
                            chunk.Coordinate,
                            chunkSize,
                            y,
                            z,
                            out voxel))
                    {
                        cache[
                            CacheIndex(
                                chunkSize + 1,
                                y + 1,
                                z + 1,
                                cacheSize
                            )
                        ] = voxel;
                    }
                }
            }

            // Borde Y-
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    Voxel voxel;

                    if (ChunkNeighborAccess.TryGetVoxel(
                            storage,
                            chunk.Coordinate,
                            x,
                            -1,
                            z,
                            out voxel))
                    {
                        cache[
                            CacheIndex(
                                x + 1,
                                0,
                                z + 1,
                                cacheSize
                            )
                        ] = voxel;
                    }
                }
            }

            // Borde Y+
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    Voxel voxel;

                    if (ChunkNeighborAccess.TryGetVoxel(
                            storage,
                            chunk.Coordinate,
                            x,
                            chunkSize,
                            z,
                            out voxel))
                    {
                        cache[
                            CacheIndex(
                                x + 1,
                                chunkSize + 1,
                                z + 1,
                                cacheSize
                            )
                        ] = voxel;
                    }
                }
            }

            // Borde Z-
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    Voxel voxel;

                    if (ChunkNeighborAccess.TryGetVoxel(
                            storage,
                            chunk.Coordinate,
                            x,
                            y,
                            -1,
                            out voxel))
                    {
                        cache[
                            CacheIndex(
                                x + 1,
                                y + 1,
                                0,
                                cacheSize
                            )
                        ] = voxel;
                    }
                }
            }

            // Borde Z+
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    Voxel voxel;

                    if (ChunkNeighborAccess.TryGetVoxel(
                            storage,
                            chunk.Coordinate,
                            x,
                            y,
                            chunkSize,
                            out voxel))
                    {
                        cache[
                            CacheIndex(
                                x + 1,
                                y + 1,
                                chunkSize + 1,
                                cacheSize
                            )
                        ] = voxel;
                    }
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
            Voxel[] voxelCache,
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
                    voxelCache,
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
            Voxel[] voxelCache,
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
                            voxelCache,
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

                    /*
                     * IMPORTANTE:
                     *
                     * Los fluidos no forman parte del Binary
                     * Greedy Meshing.
                     *
                     * Su geometría se genera posteriormente
                     * mediante BuildFluidGeometry().
                     */
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
                            voxelCache,
                            neighborX,
                            neighborY,
                            neighborZ,
                            cacheSize
                        );

                    if (neighborId != BlockIds.Air)
                    {
                        if (!blockDatabase.TryGet(
                                neighborId,
                                out BlockRuntimeData neighborBlock))
                        {
                            throw new InvalidOperationException(
                                $"VoxelMeshBuilder no pudo resolver " +
                                $"el BlockId vecino={neighborId}."
                            );
                        }

                        /*
                         * Los fluidos no deben considerarse
                         * sólidos que oculten la cara del cubo.
                         *
                         * OccludesFaces sigue siendo la autoridad
                         * para bloques sólidos.
                         */
                        if (neighborBlock.OccludesFaces)
                            continue;
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

        /*
         * ============================================================
         * FLUID MESH
         * ============================================================
         *
         * Los fluidos utilizan Voxel.State como nivel:
         *
         * 0  = vacío
         * 1  = 1/15 de altura
         * ...
         * 15 = bloque completamente lleno
         *
         * No utilizamos greedy meshing aquí porque dos voxels
         * contiguos pueden tener alturas diferentes.
         */
        private void BuildFluidGeometry(
            ChunkMeshData mesh,
            Chunk chunk,
            ChunkStorage storage,
            Voxel[] voxelCache)
        {
            BuildFluidHorizontalFaces(
                mesh,
                chunk,
                storage,
                voxelCache,
                true
            );

            BuildFluidHorizontalFaces(
                mesh,
                chunk,
                storage,
                voxelCache,
                false
            );

            BuildFluidSideFaces(
                mesh,
                chunk,
                storage,
                voxelCache,
                VoxelFace.North
            );

            BuildFluidSideFaces(
                mesh,
                chunk,
                storage,
                voxelCache,
                VoxelFace.South
            );

            BuildFluidSideFaces(
                mesh,
                chunk,
                storage,
                voxelCache,
                VoxelFace.East
            );

            BuildFluidSideFaces(
                mesh,
                chunk,
                storage,
                voxelCache,
                VoxelFace.West
            );
        }

        private void BuildFluidHorizontalFaces(
            ChunkMeshData mesh,
            Chunk chunk,
            ChunkStorage storage,
            Voxel[] voxelCache,
            bool top)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            int cellCount =
                chunkSize * chunkSize;

            bool[] visible =
                new bool[cellCount];

            ushort[] blockIds =
                new ushort[cellCount];

            byte[] biomeIds =
                new byte[cellCount];

            int[] textureKeys =
                new int[cellCount];

            float[] heights =
                new float[cellCount];

            for (int y = 0; y < chunkSize; y++)
            {
                Array.Clear(
                    visible,
                    0,
                    visible.Length
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

                Array.Clear(
                    textureKeys,
                    0,
                    textureKeys.Length
                );

                Array.Clear(
                    heights,
                    0,
                    heights.Length
                );

                for (int z = 0; z < chunkSize; z++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        int index =
                            x +
                            z * chunkSize;

                        Voxel voxel =
                            GetCachedVoxel(
                                voxelCache,
                                x,
                                y,
                                z
                            );

                        if (!TryGetFluidBlock(
                                voxel,
                                out BlockRuntimeData block))
                        {
                            continue;
                        }

                        float height =
                            GetFluidHeight(
                                voxel
                            );

                        if (height <= 0f)
                            continue;

                        bool faceVisible =
                            top
                                ? IsFluidTopFaceVisible(
                                    voxelCache,
                                    chunk,
                                    storage,
                                    x,
                                    y,
                                    z,
                                    voxel
                                )
                                : IsFluidBottomFaceVisible(
                                    voxelCache,
                                    chunk,
                                    storage,
                                    x,
                                    y,
                                    z,
                                    voxel
                                );

                        if (!faceVisible)
                            continue;

                        VoxelFace face =
                            top
                                ? VoxelFace.Top
                                : VoxelFace.Bottom;

                        BiomeId biomeId =
                            GetFluidBiomeId(
                                chunk,
                                x,
                                z
                            );

                        visible[index] = true;

                        blockIds[index] =
                            voxel.BlockId;

                        biomeIds[index] =
                            (byte)biomeId;

                        textureKeys[index] =
                            GetTextureKey(
                                block,
                                face
                            );

                        heights[index] =
                            height;
                    }
                }

                BinaryGreedyMergeFluidHorizontal(
                    mesh,
                    visible,
                    blockIds,
                    biomeIds,
                    textureKeys,
                    heights,
                    y,
                    top
                );
            }
        }

        private void BinaryGreedyMergeFluidHorizontal(
            ChunkMeshData mesh,
            bool[] visible,
            ushort[] blockIds,
            byte[] biomeIds,
            int[] textureKeys,
            float[] heights,
            int y,
            bool top)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            bool[] consumed =
                new bool[
                    chunkSize * chunkSize
                ];

            for (int z = 0; z < chunkSize; z++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    int index =
                        x +
                        z * chunkSize;

                    if (consumed[index] ||
                        !visible[index])
                    {
                        continue;
                    }

                    ushort blockId =
                        blockIds[index];

                    byte biomeId =
                        biomeIds[index];

                    int textureKey =
                        textureKeys[index];

                    float height =
                        heights[index];

                    int width = 1;

                    while (
                        x + width < chunkSize)
                    {
                        int next =
                            (x + width) +
                            z * chunkSize;

                        if (consumed[next] ||
                            !visible[next])
                        {
                            break;
                        }

                        if (blockIds[next] != blockId ||
                            biomeIds[next] != biomeId ||
                            textureKeys[next] != textureKey ||
                            !Mathf.Approximately(
                                heights[next],
                                height
                            ))
                        {
                            break;
                        }

                        width++;
                    }

                    int depth = 1;

                    bool canExpand = true;

                    while (
                        z + depth < chunkSize &&
                        canExpand)
                    {
                        for (
                            int dx = 0;
                            dx < width;
                            dx++)
                        {
                            int test =
                                (x + dx) +
                                (z + depth) * chunkSize;

                            if (consumed[test] ||
                                !visible[test])
                            {
                                canExpand = false;
                                break;
                            }

                            if (blockIds[test] != blockId ||
                                biomeIds[test] != biomeId ||
                                textureKeys[test] != textureKey ||
                                !Mathf.Approximately(
                                    heights[test],
                                    height
                                ))
                            {
                                canExpand = false;
                                break;
                            }
                        }

                        if (canExpand)
                            depth++;
                    }

                    for (
                        int dz = 0;
                        dz < depth;
                        dz++)
                    {
                        for (
                            int dx = 0;
                            dx < width;
                            dx++)
                        {
                            consumed[
                                (x + dx) +
                                (z + dz) * chunkSize
                            ] = true;
                        }
                    }

                    if (!blockDatabase.TryGet(
                            blockId,
                            out BlockRuntimeData block))
                    {
                        throw new InvalidOperationException(
                            $"VoxelMeshBuilder no encontró " +
                            $"el BlockRuntimeData del fluido. " +
                            $"BlockId={blockId}."
                        );
                    }

                    BiomeId biome =
                        (BiomeId)biomeId;

                    AtlasTileCoordinate texture =
                        GetTexture(
                            block,
                            top
                                ? VoxelFace.Top
                                : VoxelFace.Bottom
                        );

                    Color32 tint =
                        GetBlockTint(
                            blockId,
                            biome
                        );

                    AddFluidHorizontalGreedyFace(
                        mesh,
                        x,
                        z,
                        y,
                        width,
                        depth,
                        height,
                        texture,
                        tint,
                        top
                    );
                }
            }
        }

        private bool IsFluidTopFaceVisible(
            Voxel[] voxelCache,
            Chunk chunk,
            ChunkStorage storage,
            int x,
            int y,
            int z,
            Voxel voxel)
        {
            if (!TryGetVoxel(
                    voxelCache,
                    storage,
                    chunk,
                    x,
                    y + 1,
                    z,
                    out Voxel above))
            {
                return true;
            }

            if (above.BlockId == voxel.BlockId &&
                above.State > 0)
            {
                return false;
            }

            if (above.BlockId != BlockIds.Air)
            {
                if (!blockDatabase.TryGet(
                        above.BlockId,
                        out BlockRuntimeData aboveBlock))
                {
                    throw new InvalidOperationException(
                        $"VoxelMeshBuilder encontró un BlockId inválido " +
                        $"al comprobar la cara superior de un fluido. " +
                        $"BlockId={above.BlockId}."
                    );
                }

                if (aboveBlock.OccludesFaces)
                    return false;
            }

            return true;
        }

        private bool IsFluidBottomFaceVisible(
            Voxel[] voxelCache,
            Chunk chunk,
            ChunkStorage storage,
            int x,
            int y,
            int z,
            Voxel voxel)
        {
            if (!TryGetVoxel(
                    voxelCache,
                    storage,
                    chunk,
                    x,
                    y - 1,
                    z,
                    out Voxel below))
            {
                return true;
            }

            if (below.BlockId == voxel.BlockId &&
                below.State > 0)
            {
                return false;
            }

            if (below.BlockId != BlockIds.Air)
            {
                if (!blockDatabase.TryGet(
                        below.BlockId,
                        out BlockRuntimeData belowBlock))
                {
                    throw new InvalidOperationException(
                        $"VoxelMeshBuilder encontró un BlockId inválido " +
                        $"al comprobar la cara inferior de un fluido. " +
                        $"BlockId={below.BlockId}."
                    );
                }

                if (belowBlock.OccludesFaces)
                    return false;
            }

            return true;
        }

        private void AddFluidHorizontalGreedyFace(
            ChunkMeshData mesh,
            int x,
            int z,
            int y,
            int width,
            int depth,
            float height,
            AtlasTileCoordinate texture,
            Color32 tint,
            bool top)
        {
            float2 atlasTileMin =
                GetAtlasTileMin(texture);

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
                    depth
                );

            float2 uv3 =
                new float2(
                    0f,
                    depth
                );

            if (top)
            {
                float faceY =
                    y + height;

                mesh.AddTiledQuad(
                    new float3(
                        x,
                        faceY,
                        z + depth
                    ),
                    new float3(
                        x + width,
                        faceY,
                        z + depth
                    ),
                    new float3(
                        x + width,
                        faceY,
                        z
                    ),
                    new float3(
                        x,
                        faceY,
                        z
                    ),
                    atlasTileMin,
                    uv0,
                    uv1,
                    uv2,
                    uv3,
                    tint
                );

                return;
            }

            mesh.AddTiledQuad(
                new float3(
                    x,
                    y,
                    z
                ),
                new float3(
                    x + width,
                    y,
                    z
                ),
                new float3(
                    x + width,
                    y,
                    z + depth
                ),
                new float3(
                    x,
                    y,
                    z + depth
                ),
                atlasTileMin,
                uv0,
                uv1,
                uv2,
                uv3,
                tint
            );
        }

        private bool TryGetFluidBlock(
            Voxel voxel,
            out BlockRuntimeData block)
        {
            if (voxel.BlockId == BlockIds.Air)
            {
                block = default;
                return false;
            }

            if (!blockDatabase.TryGet(
                    voxel.BlockId,
                    out block))
            {
                throw new InvalidOperationException(
                    $"VoxelMeshBuilder encontró un BlockId inválido " +
                    $"durante el procesamiento de fluidos. " +
                    $"BlockId={voxel.BlockId}."
                );
            }

            return block.MeshType == BlockMeshType.Fluid &&
                block.IsFluid &&
                voxel.State > 0;
        }

        private BiomeId GetFluidBiomeId(
            Chunk chunk,
            int x,
            int z)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            int index =
                x +
                z * chunkSize;

            return chunk.BiomeData.Biomes[index];
        }

        private void BuildFluidSideFaces(
            ChunkMeshData mesh,
            Chunk chunk,
            ChunkStorage storage,
            Voxel[] voxelCache,
            VoxelFace face)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            int cellCount =
                chunkSize * chunkSize;

            bool[] visible =
                new bool[cellCount];

            ushort[] blockIds =
                new ushort[cellCount];

            byte[] biomeIds =
                new byte[cellCount];

            int[] textureKeys =
                new int[cellCount];

            float[] heights =
                new float[cellCount];

            float[] bottomHeights =
                new float[cellCount];

            for (int y = 0; y < chunkSize; y++)
            {
                Array.Clear(
                    visible,
                    0,
                    visible.Length
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

                Array.Clear(
                    textureKeys,
                    0,
                    textureKeys.Length
                );

                Array.Clear(
                    heights,
                    0,
                    heights.Length
                );

                Array.Clear(
                    bottomHeights,
                    0,
                    bottomHeights.Length
                );

                for (int v = 0; v < chunkSize; v++)
                {
                    for (int u = 0; u < chunkSize; u++)
                    {
                        GetFluidSideCoordinate(
                            face,
                            u,
                            y,
                            v,
                            out int x,
                            out int voxelY,
                            out int z
                        );

                        Voxel voxel =
                            GetCachedVoxel(
                                voxelCache,
                                x,
                                voxelY,
                                z
                            );

                        if (!TryGetFluidBlock(
                                voxel,
                                out BlockRuntimeData block))
                        {
                            continue;
                        }

                        float height =
                            GetFluidHeight(voxel);

                        if (height <= 0f)
                            continue;

                        GetFluidSideNeighbor(
                            face,
                            x,
                            voxelY,
                            z,
                            out int neighborX,
                            out int neighborY,
                            out int neighborZ
                        );

                        float neighborHeight = 0f;
                        bool sameFluid = false;
                        bool blocked = false;

                        if (TryGetVoxel(
                                voxelCache,
                                storage,
                                chunk,
                                neighborX,
                                neighborY,
                                neighborZ,
                                out Voxel neighbor))
                        {
                            if (neighbor.BlockId != BlockIds.Air)
                            {
                                if (blockDatabase.TryGet(
                                        neighbor.BlockId,
                                        out BlockRuntimeData neighborBlock))
                                {
                                    if (neighborBlock.MeshType ==
                                            BlockMeshType.Fluid &&
                                        neighborBlock.IsFluid &&
                                        neighbor.BlockId == voxel.BlockId &&
                                        neighbor.State > 0)
                                    {
                                        sameFluid = true;

                                        neighborHeight =
                                            GetFluidHeight(
                                                neighbor
                                            );
                                    }
                                    else if (neighborBlock.OccludesFaces)
                                    {
                                        blocked = true;
                                    }
                                }
                            }
                        }

                        if (blocked)
                            continue;

                        if (sameFluid &&
                            neighborHeight >= height)
                        {
                            continue;
                        }

                        float bottomHeight =
                            sameFluid
                                ? neighborHeight
                                : 0f;

                        if (bottomHeight >= height)
                            continue;

                        BiomeId biomeId =
                            GetFluidBiomeId(
                                chunk,
                                x,
                                z
                            );

                        int index =
                            u + v * chunkSize;

                        visible[index] = true;

                        blockIds[index] =
                            voxel.BlockId;

                        biomeIds[index] =
                            (byte)biomeId;

                        textureKeys[index] =
                            GetTextureKey(
                                block,
                                face
                            );

                        heights[index] =
                            height;

                        bottomHeights[index] =
                            bottomHeight;
                    }
                }

                MergeFluidSideRow(
                    mesh,
                    face,
                    y,
                    visible,
                    blockIds,
                    biomeIds,
                    textureKeys,
                    heights,
                    bottomHeights
                );
            }
        }

        private void GetFluidSideCoordinate(
            VoxelFace face,
            int u,
            int y,
            int v,
            out int x,
            out int voxelY,
            out int z)
        {
            switch (face)
            {
                case VoxelFace.North:
                case VoxelFace.South:
                    x = u;
                    voxelY = y;
                    z = v;
                    break;

                case VoxelFace.East:
                case VoxelFace.West:
                    x = v;
                    voxelY = y;
                    z = u;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(face)
                    );
            }
        }

        private void GetFluidSideNeighbor(
            VoxelFace face,
            int x,
            int y,
            int z,
            out int neighborX,
            out int neighborY,
            out int neighborZ)
        {
            neighborX = x;
            neighborY = y;
            neighborZ = z;

            switch (face)
            {
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
        }

        private void MergeFluidSideRow(
            ChunkMeshData mesh,
            VoxelFace face,
            int y,
            bool[] visible,
            ushort[] blockIds,
            byte[] biomeIds,
            int[] textureKeys,
            float[] heights,
            float[] bottomHeights)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            for (int v = 0; v < chunkSize; v++)
            {
                int u = 0;

                while (u < chunkSize)
                {
                    int index =
                        u + v * chunkSize;

                    if (!visible[index])
                    {
                        u++;
                        continue;
                    }

                    ushort blockId =
                        blockIds[index];

                    byte biomeId =
                        biomeIds[index];

                    int textureKey =
                        textureKeys[index];

                    float height =
                        heights[index];

                    float bottomHeight =
                        bottomHeights[index];

                    int width = 1;

                    while (u + width < chunkSize)
                    {
                        int nextIndex =
                            (u + width) + v * chunkSize;

                        if (!visible[nextIndex])
                            break;

                        if (blockIds[nextIndex] != blockId ||
                            biomeIds[nextIndex] != biomeId ||
                            textureKeys[nextIndex] != textureKey ||
                            !Mathf.Approximately(
                                heights[nextIndex],
                                height
                            ) ||
                            !Mathf.Approximately(
                                bottomHeights[nextIndex],
                                bottomHeight
                            ))
                        {
                            break;
                        }

                        width++;
                    }

                    BiomeId biome =
                        (BiomeId)biomeId;

                    Color32 tint =
                        GetBlockTint(
                            blockId,
                            biome
                        );

                    AtlasTileCoordinate texture;

                    if (!blockDatabase.TryGet(
                            blockId,
                            out BlockRuntimeData block))
                    {
                        throw new InvalidOperationException(
                            $"VoxelMeshBuilder no encontró " +
                            $"el BlockRuntimeData del fluido. " +
                            $"BlockId={blockId}."
                        );
                    }

                    texture =
                        GetTexture(
                            block,
                            face
                        );

                    AddFluidSideGreedyFace(
                        mesh,
                        face,
                        y,
                        u,
                        v,
                        width,
                        bottomHeight,
                        height,
                        texture,
                        tint
                    );

                    for (int i = 0; i < width; i++)
                    {
                        visible[
                            (u + i) + v * chunkSize
                        ] = false;
                    }

                    u += width;
                }
            }
        }

private void AddFluidSideGreedyFace(
    ChunkMeshData mesh,
    VoxelFace face,
    int y,
    int u,
    int v,
    int width,
    float bottomHeight,
    float height,
    AtlasTileCoordinate texture,
    Color32 tint)
{
    float2 atlasTileMin =
        GetAtlasTileMin(texture);

    float2 uv0 =
        new float2(
            0f,
            bottomHeight
        );

    float2 uv1 =
        new float2(
            width,
            bottomHeight
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

    switch (face)
    {
        case VoxelFace.North:
            mesh.AddTiledQuad(
                new float3(
                    u,
                    y + bottomHeight,
                    v + 1
                ),
                new float3(
                    u + width,
                    y + bottomHeight,
                    v + 1
                ),
                new float3(
                    u + width,
                    y + height,
                    v + 1
                ),
                new float3(
                    u,
                    y + height,
                    v + 1
                ),
                atlasTileMin,
                uv0,
                uv1,
                uv2,
                uv3,
                tint
            );
            break;

        case VoxelFace.South:
            mesh.AddTiledQuad(
                new float3(
                    u + width,
                    y + bottomHeight,
                    v
                ),
                new float3(
                    u,
                    y + bottomHeight,
                    v
                ),
                new float3(
                    u,
                    y + height,
                    v
                ),
                new float3(
                    u + width,
                    y + height,
                    v
                ),
                atlasTileMin,
                uv0,
                uv1,
                uv2,
                uv3,
                tint
            );
            break;

        case VoxelFace.East:
            mesh.AddTiledQuad(
                new float3(
                    v + 1,
                    y + bottomHeight,
                    u + width
                ),
                new float3(
                    v + 1,
                    y + bottomHeight,
                    u
                ),
                new float3(
                    v + 1,
                    y + height,
                    u
                ),
                new float3(
                    v + 1,
                    y + height,
                    u + width
                ),
                atlasTileMin,
                uv0,
                uv1,
                uv2,
                uv3,
                tint
            );
            break;

        case VoxelFace.West:
            mesh.AddTiledQuad(
                new float3(
                    v,
                    y + bottomHeight,
                    u
                ),
                new float3(
                    v,
                    y + bottomHeight,
                    u + width
                ),
                new float3(
                    v,
                    y + height,
                    u + width
                ),
                new float3(
                    v,
                    y + height,
                    u
                ),
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

        private int GetFluidMergeKey(
            int textureKey,
            ushort blockId,
            BiomeId biomeId,
            float height)
        {
            int heightKey =
                (int)(height * 255f);

            unchecked
            {
                int hash = 17;

                hash =
                    hash * 31 +
                    textureKey;

                hash =
                    hash * 31 +
                    blockId;

                hash =
                    hash * 31 +
                    (int)biomeId;

                hash =
                    hash * 31 +
                    heightKey;

                return hash == 0
                    ? 1
                    : hash;
            }
        }

        private float GetFluidHeight(
            Voxel voxel)
        {
            return Mathf.Clamp01(
                voxel.State /
                (float)FluidState.MaxLevel
            );
        }

        private void BuildFluidTopFace(
            ChunkMeshData mesh,
            Chunk chunk,
            ChunkStorage storage,
            Voxel[] voxelCache,
            int x,
            int y,
            int z,
            Voxel voxel,
            BlockRuntimeData block,
            float height,
            Color32 tint)
        {
            if (TryGetVoxel(
                    voxelCache,
                    storage,
                    chunk,
                    x,
                    y + 1,
                    z,
                    out Voxel above))
            {
                if (above.BlockId != BlockIds.Air &&
                    blockDatabase.TryGet(
                        above.BlockId,
                        out BlockRuntimeData aboveBlock))
                {
                    if (aboveBlock.MeshType == BlockMeshType.Fluid &&
                        above.BlockId == voxel.BlockId &&
                        above.State > 0)
                    {
                        return;
                    }

                    if (aboveBlock.OccludesFaces)
                        return;
                }
            }

            AtlasTileCoordinate texture =
                GetTexture(
                    block,
                    VoxelFace.Top
                );

            float y0 =
                y + height;

            float2 atlasTileMin =
                GetAtlasTileMin(texture);

            mesh.AddTiledQuad(
                new float3(
                    x,
                    y0,
                    z + 1
                ),
                new float3(
                    x + 1,
                    y0,
                    z + 1
                ),
                new float3(
                    x + 1,
                    y0,
                    z
                ),
                new float3(
                    x,
                    y0,
                    z
                ),
                atlasTileMin,
                new float2(0f, 0f),
                new float2(1f, 0f),
                new float2(1f, 1f),
                new float2(0f, 1f),
                tint
            );
        }

        private void BuildFluidBottomFace(
            ChunkMeshData mesh,
            Chunk chunk,
            ChunkStorage storage,
            Voxel[] voxelCache,
            int x,
            int y,
            int z,
            Voxel voxel,
            BlockRuntimeData block,
            float height,
            Color32 tint)
        {
            if (TryGetVoxel(
                    voxelCache,
                    storage,
                    chunk,
                    x,
                    y - 1,
                    z,
                    out Voxel below))
            {
                if (below.BlockId != BlockIds.Air &&
                    blockDatabase.TryGet(
                        below.BlockId,
                        out BlockRuntimeData belowBlock))
                {
                    if (belowBlock.MeshType == BlockMeshType.Fluid &&
                        below.BlockId == voxel.BlockId)
                    {
                        return;
                    }

                    if (belowBlock.OccludesFaces)
                        return;
                }
            }

            AtlasTileCoordinate texture =
                GetTexture(
                    block,
                    VoxelFace.Bottom
                );

            float2 atlasTileMin =
                GetAtlasTileMin(texture);

            mesh.AddTiledQuad(
                new float3(
                    x,
                    y,
                    z
                ),
                new float3(
                    x + 1,
                    y,
                    z
                ),
                new float3(
                    x + 1,
                    y,
                    z + 1
                ),
                new float3(
                    x,
                    y,
                    z + 1
                ),
                atlasTileMin,
                new float2(0f, 0f),
                new float2(1f, 0f),
                new float2(1f, 1f),
                new float2(0f, 1f),
                tint
            );
        }

        private void BuildFluidSideFace(
            ChunkMeshData mesh,
            Chunk chunk,
            ChunkStorage storage,
            Voxel[] voxelCache,
            int x,
            int y,
            int z,
            Voxel voxel,
            BlockRuntimeData block,
            float height,
            VoxelFace face,
            Color32 tint)
        {
            int neighborX = x;
            int neighborY = y;
            int neighborZ = z;

            switch (face)
            {
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

            float neighborHeight = 0f;
            bool sameFluid = false;
            bool blocked = false;

            if (TryGetVoxel(
                    voxelCache,
                    storage,
                    chunk,
                    neighborX,
                    neighborY,
                    neighborZ,
                    out Voxel neighbor))
            {
                if (neighbor.BlockId != BlockIds.Air)
                {
                    if (blockDatabase.TryGet(
                            neighbor.BlockId,
                            out BlockRuntimeData neighborBlock))
                    {
                        if (neighborBlock.MeshType == BlockMeshType.Fluid &&
                            neighbor.BlockId == voxel.BlockId &&
                            neighbor.State > 0)
                        {
                            sameFluid = true;

                            neighborHeight =
                                GetFluidHeight(
                                    neighbor
                                );
                        }
                        else if (neighborBlock.OccludesFaces)
                        {
                            blocked = true;
                        }
                    }
                }
            }

            if (blocked)
                return;

            /*
             * Si el fluido vecino tiene la misma o mayor altura,
             * nuestra cara queda completamente cubierta.
             */
            if (sameFluid &&
                neighborHeight >= height)
            {
                return;
            }

            /*
             * Si el vecino tiene el mismo fluido pero es más bajo,
             * solo queda visible la parte superior a su nivel.
             */
            float bottomHeight =
                sameFluid
                    ? neighborHeight
                    : 0f;

            if (bottomHeight >= height)
                return;

            AtlasTileCoordinate texture =
                GetTexture(
                    block,
                    face
                );

            float2 atlasTileMin =
                GetAtlasTileMin(texture);

            float y0 =
                y + bottomHeight;

            float y1 =
                y + height;

            switch (face)
            {
                case VoxelFace.North:
                    mesh.AddTiledQuad(
                        new float3(
                            x,
                            y0,
                            z + 1
                        ),
                        new float3(
                            x + 1,
                            y0,
                            z + 1
                        ),
                        new float3(
                            x + 1,
                            y1,
                            z + 1
                        ),
                        new float3(
                            x,
                            y1,
                            z + 1
                        ),
                        atlasTileMin,
                        new float2(0f, bottomHeight),
                        new float2(1f, bottomHeight),
                        new float2(1f, height),
                        new float2(0f, height),
                        tint
                    );
                    break;

                case VoxelFace.South:
                    mesh.AddTiledQuad(
                        new float3(
                            x + 1,
                            y0,
                            z
                        ),
                        new float3(
                            x,
                            y0,
                            z
                        ),
                        new float3(
                            x,
                            y1,
                            z
                        ),
                        new float3(
                            x + 1,
                            y1,
                            z
                        ),
                        atlasTileMin,
                        new float2(0f, bottomHeight),
                        new float2(1f, bottomHeight),
                        new float2(1f, height),
                        new float2(0f, height),
                        tint
                    );
                    break;

                case VoxelFace.East:
                    mesh.AddTiledQuad(
                        new float3(
                            x + 1,
                            y0,
                            z + 1
                        ),
                        new float3(
                            x + 1,
                            y0,
                            z
                        ),
                        new float3(
                            x + 1,
                            y1,
                            z
                        ),
                        new float3(
                            x + 1,
                            y1,
                            z + 1
                        ),
                        atlasTileMin,
                        new float2(0f, bottomHeight),
                        new float2(1f, bottomHeight),
                        new float2(1f, height),
                        new float2(0f, height),
                        tint
                    );
                    break;

                case VoxelFace.West:
                    mesh.AddTiledQuad(
                        new float3(
                            x,
                            y0,
                            z
                        ),
                        new float3(
                            x,
                            y0,
                            z + 1
                        ),
                        new float3(
                            x,
                            y1,
                            z + 1
                        ),
                        new float3(
                            x,
                            y1,
                            z
                        ),
                        atlasTileMin,
                        new float2(0f, bottomHeight),
                        new float2(1f, bottomHeight),
                        new float2(1f, height),
                        new float2(0f, height),
                        tint
                    );
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(face)
                    );
            }
        }

        private bool TryGetVoxel(
            Voxel[] voxelCache,
            ChunkStorage storage,
            Chunk chunk,
            int x,
            int y,
            int z,
            out Voxel voxel)
        {
            if (x >= -1 &&
                x <= VoxelConstants.ChunkSize &&
                y >= -1 &&
                y <= VoxelConstants.ChunkSize &&
                z >= -1 &&
                z <= VoxelConstants.ChunkSize)
            {
                voxel =
                    GetCachedVoxel(
                        voxelCache,
                        x,
                        y,
                        z
                    );

                return true;
            }

            return ChunkNeighborAccess.TryGetVoxel(
                storage,
                chunk.Coordinate,
                x,
                y,
                z,
                out voxel
            );
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
            Voxel[] cache,
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
            ].BlockId;
        }

        private Voxel GetCachedVoxel(
            Voxel[] cache,
            int x,
            int y,
            int z)
        {
            int cacheSize =
                VoxelConstants.ChunkSize + 2;

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
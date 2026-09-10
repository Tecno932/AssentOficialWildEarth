using System;
using UnityEngine;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    public sealed class VoxelMeshBuilder
    {
        private const int AtlasSize = 512;
        private const int TileSize = 16;
        private const int AtlasTilesPerAxis =
            AtlasSize / TileSize;

        private readonly BlockRuntimeDatabase blockDatabase;

        /*
         * ============================================================
         * DEBUG
         * ============================================================
         *
         * true:
         *   imprime información del primer voxel sólido encontrado.
         *
         * false:
         *   funcionamiento normal sin logs.
         */
        private const bool DebugTextureData = true;

        private bool debugPrinted;

        public VoxelMeshBuilder(
            BlockRuntimeDatabase blockDatabase)
        {
            this.blockDatabase =
                blockDatabase ??
                throw new ArgumentNullException(
                    nameof(blockDatabase)
                );
        }

        public ChunkMeshData Build(
            Chunk chunk,
            ChunkStorage storage)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(
                    nameof(chunk)
                );
            }

            if (storage == null)
            {
                throw new ArgumentNullException(
                    nameof(storage)
                );
            }

            if (!chunk.Data.IsCreated)
            {
                throw new InvalidOperationException(
                    $"El chunk {chunk.Coordinate} no tiene datos válidos."
                );
            }

            debugPrinted = false;

            ChunkMeshData mesh =
                new ChunkMeshData();

            int chunkSize =
                VoxelConstants.ChunkSize;

            int solidVoxelCount = 0;

            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        Voxel voxel =
                            ChunkDataAccess.GetVoxel(
                                chunk.Data,
                                x,
                                y,
                                z
                            );

                        if (voxel.IsAir)
                            continue;

                        solidVoxelCount++;

                        BlockRuntimeData block =
                            blockDatabase.Get(
                                voxel.BlockId
                            );

                        if (DebugTextureData &&
                            !debugPrinted)
                        {
                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Chunk={chunk.Coordinate} " +
                                $"Voxel=({x},{y},{z}) " +
                                $"BlockId={voxel.BlockId} " +
                                $"MeshType={block.MeshType} " +
                                $"TopTexture={block.TopTexture} " +
                                $"BottomTexture={block.BottomTexture} " +
                                $"SideTexture={block.SideTexture}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Top UVs: " +
                                $"A={GetAtlasUV(block.TopTexture, 0)} " +
                                $"B={GetAtlasUV(block.TopTexture, 1)} " +
                                $"C={GetAtlasUV(block.TopTexture, 2)} " +
                                $"D={GetAtlasUV(block.TopTexture, 3)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Bottom UVs: " +
                                $"A={GetAtlasUV(block.BottomTexture, 0)} " +
                                $"B={GetAtlasUV(block.BottomTexture, 1)} " +
                                $"C={GetAtlasUV(block.BottomTexture, 2)} " +
                                $"D={GetAtlasUV(block.BottomTexture, 3)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Side UVs: " +
                                $"A={GetAtlasUV(block.SideTexture, 0)} " +
                                $"B={GetAtlasUV(block.SideTexture, 1)} " +
                                $"C={GetAtlasUV(block.SideTexture, 2)} " +
                                $"D={GetAtlasUV(block.SideTexture, 3)}"
                            );

                            debugPrinted = true;
                        }

                        if (block.MeshType !=
                            BlockMeshType.Cube)
                        {
                            continue;
                        }

                        BuildCube(
                            mesh,
                            chunk,
                            storage,
                            x,
                            y,
                            z,
                            block
                        );
                    }
                }
            }

            if (DebugTextureData)
            {
                Debug.Log(
                    "[VoxelTextureDebug] " +
                    $"Chunk={chunk.Coordinate} " +
                    $"SolidVoxels={solidVoxelCount} " +
                    $"Vertices={mesh.VertexCount} " +
                    $"UVs={mesh.UVs.Count} " +
                    $"Triangles={mesh.Triangles.Count}"
                );

                if (mesh.VertexCount != mesh.UVs.Count)
                {
                    Debug.LogError(
                        "[VoxelTextureDebug] ERROR: " +
                        $"VertexCount ({mesh.VertexCount}) " +
                        "!= UVCount " +
                        $"({mesh.UVs.Count})"
                    );
                }
            }

            return mesh;
        }

        private void BuildCube(
            ChunkMeshData mesh,
            Chunk chunk,
            ChunkStorage storage,
            int x,
            int y,
            int z,
            BlockRuntimeData block)
        {
            if (IsFaceVisible(
                    storage,
                    chunk,
                    x,
                    y,
                    z,
                    VoxelFace.Bottom))
            {
                AddBottomFace(
                    mesh,
                    x,
                    y,
                    z,
                    block.BottomTexture
                );
            }

            if (IsFaceVisible(
                    storage,
                    chunk,
                    x,
                    y,
                    z,
                    VoxelFace.Top))
            {
                AddTopFace(
                    mesh,
                    x,
                    y,
                    z,
                    block.TopTexture
                );
            }

            if (IsFaceVisible(
                    storage,
                    chunk,
                    x,
                    y,
                    z,
                    VoxelFace.North))
            {
                AddNorthFace(
                    mesh,
                    x,
                    y,
                    z,
                    block.SideTexture
                );
            }

            if (IsFaceVisible(
                    storage,
                    chunk,
                    x,
                    y,
                    z,
                    VoxelFace.South))
            {
                AddSouthFace(
                    mesh,
                    x,
                    y,
                    z,
                    block.SideTexture
                );
            }

            if (IsFaceVisible(
                    storage,
                    chunk,
                    x,
                    y,
                    z,
                    VoxelFace.East))
            {
                AddEastFace(
                    mesh,
                    x,
                    y,
                    z,
                    block.SideTexture
                );
            }

            if (IsFaceVisible(
                    storage,
                    chunk,
                    x,
                    y,
                    z,
                    VoxelFace.West))
            {
                AddWestFace(
                    mesh,
                    x,
                    y,
                    z,
                    block.SideTexture
                );
            }
        }

        private bool IsFaceVisible(
            ChunkStorage storage,
            Chunk chunk,
            int x,
            int y,
            int z,
            VoxelFace face)
        {
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
                        nameof(face),
                        face,
                        "Cara de voxel inválida."
                    );
            }

            bool resolved =
                ChunkNeighborAccess.TryGetVoxel(
                    storage,
                    chunk.Coordinate,
                    neighborX,
                    neighborY,
                    neighborZ,
                    out Voxel neighbor
                );

            if (!resolved)
                return true;

            if (neighbor.IsAir)
                return true;

            if (!blockDatabase.TryGet(
                    neighbor.BlockId,
                    out BlockRuntimeData neighborBlock))
            {
                return true;
            }

            return !neighborBlock.OccludesFaces;
        }

        private static void AddBottomFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z,
            int textureIndex)
        {
            mesh.AddQuad(
                new float3(x, y, z),
                new float3(x + 1, y, z),
                new float3(x + 1, y, z + 1),
                new float3(x, y, z + 1),

                GetAtlasUV(textureIndex, 0),
                GetAtlasUV(textureIndex, 1),
                GetAtlasUV(textureIndex, 2),
                GetAtlasUV(textureIndex, 3)
            );
        }

        private static void AddTopFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z,
            int textureIndex)
        {
            mesh.AddQuad(
                new float3(x, y + 1, z + 1),
                new float3(x + 1, y + 1, z + 1),
                new float3(x + 1, y + 1, z),
                new float3(x, y + 1, z),

                GetAtlasUV(textureIndex, 0),
                GetAtlasUV(textureIndex, 1),
                GetAtlasUV(textureIndex, 2),
                GetAtlasUV(textureIndex, 3)
            );
        }

        private static void AddNorthFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z,
            int textureIndex)
        {
            mesh.AddQuad(
                new float3(x, y, z + 1),
                new float3(x + 1, y, z + 1),
                new float3(x + 1, y + 1, z + 1),
                new float3(x, y + 1, z + 1),

                GetAtlasUV(textureIndex, 0),
                GetAtlasUV(textureIndex, 1),
                GetAtlasUV(textureIndex, 2),
                GetAtlasUV(textureIndex, 3)
            );
        }

        private static void AddSouthFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z,
            int textureIndex)
        {
            mesh.AddQuad(
                new float3(x + 1, y, z),
                new float3(x, y, z),
                new float3(x, y + 1, z),
                new float3(x + 1, y + 1, z),

                GetAtlasUV(textureIndex, 0),
                GetAtlasUV(textureIndex, 1),
                GetAtlasUV(textureIndex, 2),
                GetAtlasUV(textureIndex, 3)
            );
        }

        private static void AddEastFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z,
            int textureIndex)
        {
            mesh.AddQuad(
                new float3(x + 1, y, z + 1),
                new float3(x + 1, y, z),
                new float3(x + 1, y + 1, z),
                new float3(x + 1, y + 1, z + 1),

                GetAtlasUV(textureIndex, 0),
                GetAtlasUV(textureIndex, 1),
                GetAtlasUV(textureIndex, 2),
                GetAtlasUV(textureIndex, 3)
            );
        }

        private static void AddWestFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z,
            int textureIndex)
        {
            mesh.AddQuad(
                new float3(x, y, z),
                new float3(x, y, z + 1),
                new float3(x, y + 1, z + 1),
                new float3(x, y + 1, z),

                GetAtlasUV(textureIndex, 0),
                GetAtlasUV(textureIndex, 1),
                GetAtlasUV(textureIndex, 2),
                GetAtlasUV(textureIndex, 3)
            );
        }

        private static float2 GetAtlasUV(
            int textureIndex,
            int corner)
        {
            if (textureIndex < 0 ||
                textureIndex >=
                AtlasTilesPerAxis *
                AtlasTilesPerAxis)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(textureIndex),
                    textureIndex,
                    "Índice de textura fuera del atlas."
                );
            }

            int tileX =
                textureIndex %
                AtlasTilesPerAxis;

            int tileY =
                textureIndex /
                AtlasTilesPerAxis;

            float tileSize =
                1f /
                AtlasTilesPerAxis;

            float uMin =
                tileX * tileSize;

            float uMax =
                (tileX + 1) * tileSize;

            float vMin =
                tileY * tileSize;

            float vMax =
                (tileY + 1) * tileSize;

            switch (corner)
            {
                case 0:
                    return new float2(
                        uMin,
                        vMin
                    );

                case 1:
                    return new float2(
                        uMax,
                        vMin
                    );

                case 2:
                    return new float2(
                        uMax,
                        vMax
                    );

                case 3:
                    return new float2(
                        uMin,
                        vMax
                    );

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(corner)
                    );
            }
        }
    }
}
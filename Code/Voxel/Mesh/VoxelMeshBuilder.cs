using System;
using UnityEngine;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    public sealed class VoxelMeshBuilder
    {
        private readonly BlockRuntimeDatabase blockDatabase;
        private readonly VoxelAtlasSettings atlasSettings;

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
                throw new ArgumentNullException(nameof(chunk));

            if (storage == null)
                throw new ArgumentNullException(nameof(storage));

            if (!chunk.Data.IsCreated)
            {
                throw new InvalidOperationException(
                    $"El chunk {chunk.Coordinate} no tiene datos válidos."
                );
            }

            ChunkMeshData mesh =
                new ChunkMeshData();

            int chunkSize =
                VoxelConstants.ChunkSize;

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

                        if (!blockDatabase.TryGet(
                                voxel.BlockId,
                                out BlockRuntimeData block))
                        {
                            throw new InvalidOperationException(
                                $"VoxelMeshBuilder encontró un BlockId inválido. " +
                                $"Chunk={chunk.Coordinate}, " +
                                $"Voxel=({x},{y},{z}), " +
                                $"BlockId={voxel.BlockId}, " +
                                $"BlockDatabaseLength={blockDatabase.Length}."
                            );
                        }

                        if (block.MeshType != BlockMeshType.Cube)
                            continue;

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

        private void AddBottomFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z,
            AtlasTileCoordinate texture)
        {
            mesh.AddQuad(
                new float3(x, y, z),
                new float3(x + 1, y, z),
                new float3(x + 1, y, z + 1),
                new float3(x, y, z + 1),

                GetAtlasUV(texture, 0),
                GetAtlasUV(texture, 1),
                GetAtlasUV(texture, 2),
                GetAtlasUV(texture, 3)
            );
        }

        private void AddTopFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z,
            AtlasTileCoordinate texture)
        {
            mesh.AddQuad(
                new float3(x, y + 1, z + 1),
                new float3(x + 1, y + 1, z + 1),
                new float3(x + 1, y + 1, z),
                new float3(x, y + 1, z),

                GetAtlasUV(texture, 0),
                GetAtlasUV(texture, 1),
                GetAtlasUV(texture, 2),
                GetAtlasUV(texture, 3)
            );
        }

        private void AddNorthFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z,
            AtlasTileCoordinate texture)
        {
            mesh.AddQuad(
                new float3(x, y, z + 1),
                new float3(x + 1, y, z + 1),
                new float3(x + 1, y + 1, z + 1),
                new float3(x, y + 1, z + 1),

                GetAtlasUV(texture, 0),
                GetAtlasUV(texture, 1),
                GetAtlasUV(texture, 2),
                GetAtlasUV(texture, 3)
            );
        }

        private void AddSouthFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z,
            AtlasTileCoordinate texture)
        {
            mesh.AddQuad(
                new float3(x + 1, y, z),
                new float3(x, y, z),
                new float3(x, y + 1, z),
                new float3(x + 1, y + 1, z),

                GetAtlasUV(texture, 0),
                GetAtlasUV(texture, 1),
                GetAtlasUV(texture, 2),
                GetAtlasUV(texture, 3)
            );
        }

        private void AddEastFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z,
            AtlasTileCoordinate texture)
        {
            mesh.AddQuad(
                new float3(x + 1, y, z + 1),
                new float3(x + 1, y, z),
                new float3(x + 1, y + 1, z),
                new float3(x + 1, y + 1, z + 1),

                GetAtlasUV(texture, 0),
                GetAtlasUV(texture, 1),
                GetAtlasUV(texture, 2),
                GetAtlasUV(texture, 3)
            );
        }

        private void AddWestFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z,
            AtlasTileCoordinate texture)
        {
            mesh.AddQuad(
                new float3(x, y, z),
                new float3(x, y, z + 1),
                new float3(x, y + 1, z + 1),
                new float3(x, y + 1, z),

                GetAtlasUV(texture, 0),
                GetAtlasUV(texture, 1),
                GetAtlasUV(texture, 2),
                GetAtlasUV(texture, 3)
            );
        }

        private float2 GetAtlasUV(
            AtlasTileCoordinate texture,
            int corner)
        {
            Vector2 uv =
                atlasSettings.GetUV(
                    texture,
                    corner
                );

            return new float2(
                uv.x,
                uv.y
            );
        }
    }
}
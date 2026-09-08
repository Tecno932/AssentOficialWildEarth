using System;

namespace WildEarth.Voxel
{
    public sealed class VoxelMeshBuilder
    {
        private readonly BlockRuntimeDatabase blockDatabase;

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

                        BlockRuntimeData block =
                            blockDatabase.Get(
                                voxel.BlockId
                            );

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
                    z
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
                    z
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
                    z
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
                    z
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
                    z
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
                    z
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
            int z)
        {
            mesh.AddQuad(
                new Unity.Mathematics.float3(
                    x, y, z
                ),
                new Unity.Mathematics.float3(
                    x + 1, y, z
                ),
                new Unity.Mathematics.float3(
                    x + 1, y, z + 1
                ),
                new Unity.Mathematics.float3(
                    x, y, z + 1
                ),
                new Unity.Mathematics.float2(0, 0),
                new Unity.Mathematics.float2(1, 0),
                new Unity.Mathematics.float2(1, 1),
                new Unity.Mathematics.float2(0, 1)
            );
        }

        private static void AddTopFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z)
        {
            mesh.AddQuad(
                new Unity.Mathematics.float3(
                    x, y + 1, z + 1
                ),
                new Unity.Mathematics.float3(
                    x + 1, y + 1, z + 1
                ),
                new Unity.Mathematics.float3(
                    x + 1, y + 1, z
                ),
                new Unity.Mathematics.float3(
                    x, y + 1, z
                ),
                new Unity.Mathematics.float2(0, 0),
                new Unity.Mathematics.float2(1, 0),
                new Unity.Mathematics.float2(1, 1),
                new Unity.Mathematics.float2(0, 1)
            );
        }

        private static void AddNorthFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z)
        {
            mesh.AddQuad(
                new Unity.Mathematics.float3(
                    x, y, z + 1
                ),
                new Unity.Mathematics.float3(
                    x + 1, y, z + 1
                ),
                new Unity.Mathematics.float3(
                    x + 1, y + 1, z + 1
                ),
                new Unity.Mathematics.float3(
                    x, y + 1, z + 1
                ),
                new Unity.Mathematics.float2(0, 0),
                new Unity.Mathematics.float2(1, 0),
                new Unity.Mathematics.float2(1, 1),
                new Unity.Mathematics.float2(0, 1)
            );
        }

        private static void AddSouthFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z)
        {
            mesh.AddQuad(
                new Unity.Mathematics.float3(
                    x + 1, y, z
                ),
                new Unity.Mathematics.float3(
                    x, y, z
                ),
                new Unity.Mathematics.float3(
                    x, y + 1, z
                ),
                new Unity.Mathematics.float3(
                    x + 1, y + 1, z
                ),
                new Unity.Mathematics.float2(0, 0),
                new Unity.Mathematics.float2(1, 0),
                new Unity.Mathematics.float2(1, 1),
                new Unity.Mathematics.float2(0, 1)
            );
        }

        private static void AddEastFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z)
        {
            mesh.AddQuad(
                new Unity.Mathematics.float3(
                    x + 1, y, z + 1
                ),
                new Unity.Mathematics.float3(
                    x + 1, y, z
                ),
                new Unity.Mathematics.float3(
                    x + 1, y + 1, z
                ),
                new Unity.Mathematics.float3(
                    x + 1, y + 1, z + 1
                ),
                new Unity.Mathematics.float2(0, 0),
                new Unity.Mathematics.float2(1, 0),
                new Unity.Mathematics.float2(1, 1),
                new Unity.Mathematics.float2(0, 1)
            );
        }

        private static void AddWestFace(
            ChunkMeshData mesh,
            int x,
            int y,
            int z)
        {
            mesh.AddQuad(
                new Unity.Mathematics.float3(
                    x, y, z
                ),
                new Unity.Mathematics.float3(
                    x, y, z + 1
                ),
                new Unity.Mathematics.float3(
                    x, y + 1, z + 1
                ),
                new Unity.Mathematics.float3(
                    x, y + 1, z
                ),
                new Unity.Mathematics.float2(0, 0),
                new Unity.Mathematics.float2(1, 0),
                new Unity.Mathematics.float2(1, 1),
                new Unity.Mathematics.float2(0, 1)
            );
        }
    }
}
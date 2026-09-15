using System;
using UnityEngine;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    public sealed class VoxelMeshBuilder
    {
        private readonly BlockRuntimeDatabase blockDatabase;
        private readonly VoxelAtlasSettings atlasSettings;

        private const bool DebugTextureData = true;

        private bool debugPrinted;

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

                        if (DebugTextureData && !debugPrinted)
                        {
                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                "========== VOXEL TEXTURE DEBUG =========="
                            );

                            // --------------------------------------------------
                            // VOXEL
                            // --------------------------------------------------

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                "========== VOXEL =========="
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Chunk={chunk.Coordinate}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Voxel=({x},{y},{z})"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"BlockId={voxel.BlockId}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"BlockIdHex=0x{voxel.BlockId:X4}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"IsAir={voxel.IsAir}"
                            );

                            // --------------------------------------------------
                            // BLOCK RUNTIME DATA
                            // --------------------------------------------------

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                "========== BLOCK =========="
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"BlockId={block.Id}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"MeshType={block.MeshType}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Flags={block.Flags}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"IsSolid={block.IsSolid}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"IsTransparent={block.IsTransparent}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"IsFluid={block.IsFluid}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"IsCollidable={block.IsCollidable}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"OccludesFaces={block.OccludesFaces}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"IsCutout={block.IsCutout}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"IsCaveCarvable={block.IsCaveCarvable}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Hardness={block.Hardness}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"LightEmission={block.LightEmission}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"RequiredTool={block.RequiredTool}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"RequiredToolLevel={block.RequiredToolLevel}"
                            );

                            // --------------------------------------------------
                            // ATLAS CONFIGURATION
                            // --------------------------------------------------

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                "========== ATLAS =========="
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"AtlasSize=" +
                                $"{atlasSettings.AtlasWidth}x" +
                                $"{atlasSettings.AtlasHeight}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"CellSize=" +
                                $"{atlasSettings.CellWidth}x" +
                                $"{atlasSettings.CellHeight}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Columns={atlasSettings.Columns}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Rows={atlasSettings.Rows}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"TileCount={atlasSettings.TileCount}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"RowZeroIsTop={atlasSettings.RowZeroIsTop}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"TileSizeUV={atlasSettings.GetTileSizeUV()}"
                            );

                            // --------------------------------------------------
                            // TOP TEXTURE
                            // --------------------------------------------------

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                "========== TOP TEXTURE =========="
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Top Row={block.TopTexture.Row}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Top Column={block.TopTexture.Column}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Top Tile=" +
                                $"({block.TopTexture.Column}," +
                                $"{block.TopTexture.Row})"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Top MinUV=" +
                                $"{atlasSettings.GetTileMinUV(block.TopTexture)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Top MaxUV=" +
                                $"{atlasSettings.GetTileMaxUV(block.TopTexture)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Top UV A=" +
                                $"{GetAtlasUV(block.TopTexture, 0)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Top UV B=" +
                                $"{GetAtlasUV(block.TopTexture, 1)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Top UV C=" +
                                $"{GetAtlasUV(block.TopTexture, 2)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Top UV D=" +
                                $"{GetAtlasUV(block.TopTexture, 3)}"
                            );

                            // --------------------------------------------------
                            // BOTTOM TEXTURE
                            // --------------------------------------------------

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                "========== BOTTOM TEXTURE =========="
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Bottom Row={block.BottomTexture.Row}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Bottom Column={block.BottomTexture.Column}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Bottom Tile=" +
                                $"({block.BottomTexture.Column}," +
                                $"{block.BottomTexture.Row})"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Bottom MinUV=" +
                                $"{atlasSettings.GetTileMinUV(block.BottomTexture)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Bottom MaxUV=" +
                                $"{atlasSettings.GetTileMaxUV(block.BottomTexture)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Bottom UV A=" +
                                $"{GetAtlasUV(block.BottomTexture, 0)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Bottom UV B=" +
                                $"{GetAtlasUV(block.BottomTexture, 1)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Bottom UV C=" +
                                $"{GetAtlasUV(block.BottomTexture, 2)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Bottom UV D=" +
                                $"{GetAtlasUV(block.BottomTexture, 3)}"
                            );

                            // --------------------------------------------------
                            // SIDE TEXTURE
                            // --------------------------------------------------

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                "========== SIDE TEXTURE =========="
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Side Row={block.SideTexture.Row}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Side Column={block.SideTexture.Column}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Side Tile=" +
                                $"({block.SideTexture.Column}," +
                                $"{block.SideTexture.Row})"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Side MinUV=" +
                                $"{atlasSettings.GetTileMinUV(block.SideTexture)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Side MaxUV=" +
                                $"{atlasSettings.GetTileMaxUV(block.SideTexture)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Side UV A=" +
                                $"{GetAtlasUV(block.SideTexture, 0)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Side UV B=" +
                                $"{GetAtlasUV(block.SideTexture, 1)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Side UV C=" +
                                $"{GetAtlasUV(block.SideTexture, 2)}"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"Side UV D=" +
                                $"{GetAtlasUV(block.SideTexture, 3)}"
                            );

                            // --------------------------------------------------
                            // FINAL SUMMARY
                            // --------------------------------------------------

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                "========== SUMMARY =========="
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                $"BlockId={voxel.BlockId} | " +
                                $"MeshType={block.MeshType} | " +
                                $"Atlas={atlasSettings.AtlasWidth}x" +
                                $"{atlasSettings.AtlasHeight} | " +
                                $"Cell={atlasSettings.CellWidth}x" +
                                $"{atlasSettings.CellHeight} | " +
                                $"Grid={atlasSettings.Columns}x" +
                                $"{atlasSettings.Rows} | " +
                                $"Top=({block.TopTexture.Column}," +
                                $"{block.TopTexture.Row}) | " +
                                $"Bottom=({block.BottomTexture.Column}," +
                                $"{block.BottomTexture.Row}) | " +
                                $"Side=({block.SideTexture.Column}," +
                                $"{block.SideTexture.Row})"
                            );

                            Debug.Log(
                                "[VoxelTextureDebug] " +
                                "=========================================="
                            );

                            debugPrinted = true;
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
using System;
using UnityEngine;

namespace WildEarth.Voxel
{
    public sealed class VoxelWorldRuntime : MonoBehaviour
    {
        [SerializeField]
        private BiomeRegistryAsset biomeRegistryAsset;

        [SerializeField]
        private BlockRegistry blockRegistry;

        [SerializeField]
        private VoxelAtlasSettings atlasSettings;

        [SerializeField]
        private OreRegistryAsset oreRegistryAsset;

        [SerializeField]
        private FluidRegistryAsset fluidRegistryAsset;

        [SerializeField]
        private VoxelWorldRenderer worldRenderer;

        [Header("World Streaming Test")]
        [SerializeField]
        [Min(0)]
        private int renderDistance = 2;

        private VoxelWorld world;
        private VoxelMeshBuilder meshBuilder;

        private bool debugVoxelCountsLogged;

        public VoxelWorld World =>
            world;

        private void Awake()
        {
            if (biomeRegistryAsset == null)
                throw new InvalidOperationException(
                    "VoxelWorldRuntime: falta BiomeRegistryAsset."
                );

            if (blockRegistry == null)
                throw new InvalidOperationException(
                    "VoxelWorldRuntime: falta BlockRegistry."
                );

            if (atlasSettings == null)
                throw new InvalidOperationException(
                    "VoxelWorldRuntime: falta VoxelAtlasSettings."
                );

            if (oreRegistryAsset == null)
                throw new InvalidOperationException(
                    "VoxelWorldRuntime: falta OreRegistryAsset."
                );

            if (fluidRegistryAsset == null)
                throw new InvalidOperationException(
                    "VoxelWorldRuntime: falta FluidRegistryAsset."
                );

            if (worldRenderer == null)
                throw new InvalidOperationException(
                    "VoxelWorldRuntime: falta VoxelWorldRenderer."
                );

            world = new VoxelWorld(
                VoxelWorldSettings.Default,
                ChunkGenerationSettings.Default,
                biomeRegistryAsset,
                blockRegistry,
                oreRegistryAsset,
                fluidRegistryAsset
            );

            world.Initialize();

            meshBuilder =
                new VoxelMeshBuilder(
                    world.BlockDatabase,
                    atlasSettings
                );

            worldRenderer.Initialize(
                world,
                meshBuilder
            );

            GenerateInitialRing();
        }

        private void GenerateInitialRing()
        {
            int chunksPerColumn =
                VoxelConstants.WorldHeight /
                VoxelConstants.ChunkSize;

            int horizontalChunkCount =
                renderDistance * 2 + 1;

            int totalColumns =
                horizontalChunkCount *
                horizontalChunkCount;

            int totalChunks =
                totalColumns *
                chunksPerColumn;

            for (int z = -renderDistance;
                z <= renderDistance;
                z++)
            {
                for (int x = -renderDistance;
                    x <= renderDistance;
                    x++)
                {
                    for (int y = 0;
                        y < chunksPerColumn;
                        y++)
                    {
                        ChunkCoordinate coordinate =
                            new ChunkCoordinate(
                                x,
                                y,
                                z
                            );

                        world.LoadAndGenerateChunk(
                            coordinate
                        );
                    }
                }
            }

            Debug.Log(
                $"[VoxelWorldRuntime] " +
                $"Columnas solicitadas: {totalColumns}, " +
                $"chunks solicitados: {totalChunks}"
            );
        }

private void Update()
{
    if (world == null)
        return;

    world.Update();

    worldRenderer.RenderCompletedChunks();

    if (!debugVoxelCountsLogged)
    {
        ChunkCoordinate debugCoordinate =
            new ChunkCoordinate(0, 3, 0);

        if (world.TryGetChunk(
                debugCoordinate,
                out Chunk chunk) &&
            chunk != null &&
            (chunk.State == ChunkState.Generated ||
            chunk.State == ChunkState.Ready))
        {
            world.DebugLogChunkBlockCounts(
                debugCoordinate
            );

            debugVoxelCountsLogged = true;
        }
    }
}

        private void OnDestroy()
        {
            if (worldRenderer != null)
                worldRenderer.Clear();

            world?.Dispose();

            world = null;
            meshBuilder = null;
        }
    }
}
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

        private VoxelWorld world;
        private VoxelMeshBuilder meshBuilder;

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

            world.LoadAndGenerateChunk(
                new ChunkCoordinate(0, 0, 0)
            );
        }

        private void Update()
        {
            if (world == null)
                return;

            world.Update();

            worldRenderer.RenderCompletedChunks();
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
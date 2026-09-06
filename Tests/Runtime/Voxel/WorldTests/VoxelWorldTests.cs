using NUnit.Framework;
using UnityEditor;
using WildEarth.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class VoxelWorldTests
    {
        private VoxelWorld world;
        private BiomeRegistryAsset biomeRegistryAsset;
        private BlockRegistry blockRegistry;
        private OreRegistryAsset oreRegistryAsset;
        private FluidRegistryAsset fluidRegistryAsset;

        [SetUp]
        public void SetUp()
        {
            biomeRegistryAsset =
                AssetDatabase.LoadAssetAtPath<BiomeRegistryAsset>(
                    "Assets/_Project/Data/Biomes/BiomeRegistry.asset"
                );

            Assert.That(
                biomeRegistryAsset,
                Is.Not.Null,
                "No se encontró BiomeRegistry.asset."
            );

            blockRegistry =
                AssetDatabase.LoadAssetAtPath<BlockRegistry>(
                    "Assets/_Project/Data/Blocks/BlockRegistry.asset"
                );

            oreRegistryAsset =
                AssetDatabase.LoadAssetAtPath<OreRegistryAsset>(
                    "Assets/_Project/Data/Ores/OreRegistry.asset"
                );

            Assert.That(
                oreRegistryAsset,
                Is.Not.Null,
                "No se encontró OreRegistry.asset."
            );

            fluidRegistryAsset =
                AssetDatabase.LoadAssetAtPath<FluidRegistryAsset>(
                    "Assets/_Project/Data/Fluids/FluidRegistry.asset"
                );

            Assert.That(
                fluidRegistryAsset,
                Is.Not.Null,
                "No se encontró FluidRegistry.asset."
            );

            Assert.That(
                blockRegistry,
                Is.Not.Null,
                "No se encontró BlockRegistry.asset."
            );

            VoxelWorldSettings worldSettings =
                new VoxelWorldSettings(
                    initialChunkPoolSize: 4,
                    maximumChunkPoolSize: 32,
                    initialChunkStorageCapacity: 32
                );

            world =
                new VoxelWorld(
                    worldSettings,
                    ChunkGenerationSettings.Default,
                    biomeRegistryAsset,
                    blockRegistry,
                    oreRegistryAsset,
                    fluidRegistryAsset
                );

            world.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            world?.Dispose();
            world = null;

            biomeRegistryAsset = null;
            blockRegistry = null;
            oreRegistryAsset = null;
            fluidRegistryAsset = null;
        }

        [Test]
        public void World_StartsInitialized()
        {
            Assert.That(
                world.IsInitialized,
                Is.True
            );
        }

        [Test]
        public void LoadChunk_CreatesChunk()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(5, 0, -2);

            Chunk chunk =
                world.LoadChunk(coordinate);

            Assert.That(
                chunk,
                Is.Not.Null
            );

            Assert.That(
                world.LoadedChunkCount,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void LoadSameChunk_ReturnsSameInstance()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(2, 0, 3);

            Chunk first =
                world.LoadChunk(coordinate);

            Chunk second =
                world.LoadChunk(coordinate);

            Assert.That(
                second,
                Is.SameAs(first)
            );

            Assert.That(
                world.LoadedChunkCount,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void UnloadChunk_RemovesChunk()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(2, 0, 3);

            world.LoadChunk(coordinate);

            bool removed =
                world.UnloadChunk(coordinate);

            Assert.That(
                removed,
                Is.True
            );

            Assert.That(
                world.LoadedChunkCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void UnloadGeneratedChunk_CompletesGenerationBeforeRemoval()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(8, 0, -3);

            Chunk chunk =
                world.LoadAndGenerateChunk(
                    coordinate
                );

            Assert.That(
                chunk.State,
                Is.EqualTo(ChunkState.Generating)
            );

            bool removed =
                world.UnloadChunk(
                    coordinate
                );

            Assert.That(
                removed,
                Is.True
            );

            Assert.That(
                world.LoadedChunkCount,
                Is.EqualTo(0)
            );
        }
    }
}
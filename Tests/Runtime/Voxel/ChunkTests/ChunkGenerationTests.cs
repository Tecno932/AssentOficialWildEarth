using NUnit.Framework;
using UnityEditor;
using WildEarth.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class ChunkGenerationTests
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

            world =
                new VoxelWorld(
                    VoxelWorldSettings.Default,
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
        public void GeneratedChunk_IsMarkedGenerated()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            Chunk chunk =
                world.LoadAndGenerateChunk(
                    coordinate
                );

            world.CompleteGeneration();

            Assert.That(
                chunk.State,
                Is.EqualTo(ChunkState.Generated)
            );

            Assert.That(
                chunk.Data.IsCreated,
                Is.True
            );
        }

        [Test]
        public void GenerationProducesNonAirBlocks()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            Chunk chunk =
                world.LoadAndGenerateChunk(
                    coordinate
                );

            world.CompleteGeneration();

            int solidVoxelCount = 0;

            for (
                int i = 0;
                i < chunk.Data.Voxels.Length;
                i++)
            {
                if (!chunk.Data.Voxels[i].IsAir)
                {
                    solidVoxelCount++;
                }
            }

            Assert.That(
                solidVoxelCount,
                Is.GreaterThan(0)
            );
        }

        [Test]
        public void SameSeedProducesSameChunk()
        {
            ChunkGenerationSettings settings =
                ChunkGenerationSettings.Default;

            VoxelWorld worldA =
                new VoxelWorld(
                    VoxelWorldSettings.Default,
                    settings,
                    biomeRegistryAsset,
                    blockRegistry,
                    oreRegistryAsset,
                    fluidRegistryAsset
                );

            VoxelWorld worldB =
                new VoxelWorld(
                    VoxelWorldSettings.Default,
                    settings,
                    biomeRegistryAsset,
                    blockRegistry,
                    oreRegistryAsset,
                    fluidRegistryAsset
                );

            try
            {
                worldA.Initialize();
                worldB.Initialize();

                ChunkCoordinate coordinate =
                    new ChunkCoordinate(
                        7,
                        0,
                        -3
                    );

                Chunk chunkA =
                    worldA.LoadAndGenerateChunk(
                        coordinate
                    );

                Chunk chunkB =
                    worldB.LoadAndGenerateChunk(
                        coordinate
                    );

                worldA.CompleteGeneration();
                worldB.CompleteGeneration();

                for (
                    int i = 0;
                    i < VoxelConstants.VoxelsPerChunk;
                    i++)
                {
                    Assert.That(
                        chunkA.Data.Voxels[i],
                        Is.EqualTo(
                            chunkB.Data.Voxels[i]
                        )
                    );
                }
            }
            finally
            {
                worldA.Dispose();
                worldB.Dispose();
            }
        }
    }
}
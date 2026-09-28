using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WildEarth.Voxel;

using VoxelData = WildEarth.Voxel.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class VoxelPersistenceBoundaryTests
    {
        private VoxelWorld world;

        private BiomeRegistryAsset biomeRegistryAsset;
        private BlockRegistry blockRegistry;
        private OreRegistryAsset oreRegistryAsset;
        private FluidRegistryAsset fluidRegistryAsset;

        private VoxelMeshBuilder builder;
        private VoxelAtlasSettings atlasSettings;

        [SetUp]
        public void SetUp()
        {
            biomeRegistryAsset =
                AssetDatabase.LoadAssetAtPath<BiomeRegistryAsset>(
                    "Assets/_Project/Data/Biomes/BiomeRegistry.asset"
                );

            blockRegistry =
                AssetDatabase.LoadAssetAtPath<BlockRegistry>(
                    "Assets/_Project/Data/Blocks/BlockRegistry.asset"
                );

            oreRegistryAsset =
                AssetDatabase.LoadAssetAtPath<OreRegistryAsset>(
                    "Assets/_Project/Data/Ores/OreRegistry.asset"
                );

            fluidRegistryAsset =
                AssetDatabase.LoadAssetAtPath<FluidRegistryAsset>(
                    "Assets/_Project/Data/Fluids/FluidRegistry.asset"
                );

            Assert.That(biomeRegistryAsset, Is.Not.Null);
            Assert.That(blockRegistry, Is.Not.Null);
            Assert.That(oreRegistryAsset, Is.Not.Null);
            Assert.That(fluidRegistryAsset, Is.Not.Null);

            atlasSettings =
                ScriptableObject.CreateInstance<VoxelAtlasSettings>();

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

            builder =
                new VoxelMeshBuilder(
                    world.BlockDatabase,
                    atlasSettings
                );
        }

        [TearDown]
        public void TearDown()
        {
            world?.Dispose();
            world = null;

            if (atlasSettings != null)
            {
                Object.DestroyImmediate(atlasSettings);
                atlasSettings = null;
            }

            biomeRegistryAsset = null;
            blockRegistry = null;
            oreRegistryAsset = null;
            fluidRegistryAsset = null;
            builder = null;
        }

        [Test]
        public void ReloadedChunk_PreservesBoundaryFaceAgainstLoadedAirNeighbor()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    8000,
                    0,
                    8000
                );

            ChunkCoordinate neighborCoordinate =
                new ChunkCoordinate(
                    8001,
                    0,
                    8000
                );

            Chunk chunk =
                world.LoadAndGenerateChunk(
                    coordinate
                );

            Chunk neighbor =
                world.LoadAndGenerateChunk(
                    neighborCoordinate
                );

            world.CompleteGeneration();

            int localX =
                VoxelConstants.ChunkSize - 1;

            int localY = 2;
            int localZ = 2;

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                localX,
                localY,
                localZ,
                new VoxelData(1)
            );

            ChunkDataAccess.SetVoxel(
                neighbor.Data,
                0,
                localY,
                localZ,
                new VoxelData(0)
            );

            int beforeReloadFaces =
                builder.Build(
                    chunk,
                    world.Chunks
                ).FaceCount;

            Assert.That(
                beforeReloadFaces,
                Is.GreaterThanOrEqualTo(6)
            );

            chunk.MarkVoxelDataChanged();

            bool unloaded =
                world.UnloadChunk(
                    coordinate
                );

            Assert.That(
                unloaded,
                Is.True
            );

            Chunk reloaded =
                world.LoadChunk(
                    coordinate
                );

            Assert.That(
                reloaded,
                Is.Not.Null
            );

            Assert.That(
                reloaded.State,
                Is.EqualTo(ChunkState.Generated)
            );

            Assert.That(
                reloaded.NeedsMesh,
                Is.True
            );

            ChunkMeshData mesh =
                builder.Build(
                    reloaded,
                    world.Chunks
                );

            try
            {
                Assert.That(
                    mesh.FaceCount,
                    Is.GreaterThanOrEqualTo(6)
                );
            }
            finally
            {
                mesh.Dispose();
            }
        }
    }
}
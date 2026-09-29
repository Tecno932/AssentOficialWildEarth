using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using WildEarth.Voxel;

using VoxelData = WildEarth.Voxel.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class VoxelWorldPersistenceTests
    {
        private VoxelWorld world;
        private string testSavePath;

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

            testSavePath =
                Path.Combine(
                    Application.temporaryCachePath,
                    "WildEarthPersistenceTests",
                    Guid.NewGuid().ToString("N")
                );

            Directory.CreateDirectory(
                testSavePath
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
                    fluidRegistryAsset,
                    testSavePath
                );

            world.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            world?.Dispose();
            world = null;

            if (!string.IsNullOrEmpty(testSavePath) &&
                Directory.Exists(testSavePath))
            {
                Directory.Delete(
                    testSavePath,
                    true
                );
            }

            testSavePath = null;

            biomeRegistryAsset = null;
            blockRegistry = null;
            oreRegistryAsset = null;
            fluidRegistryAsset = null;
        }

        [Test]
        public void ModifiedChunk_PersistsAfterUnloadAndReload()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    5000,
                    0,
                    5000
                );

            Chunk chunk =
                world.LoadAndGenerateChunk(
                    coordinate
                );

            world.CompleteGeneration();

            Assert.That(
                chunk.State,
                Is.EqualTo(ChunkState.Generated)
                    .Or.EqualTo(ChunkState.Ready)
            );

            int localX = 3;
            int localY = 2;
            int localZ = 4;

            int worldX =
                coordinate.X * VoxelConstants.ChunkSize +
                localX;

            int worldY =
                coordinate.Y * VoxelConstants.ChunkSize +
                localY;

            int worldZ =
                coordinate.Z * VoxelConstants.ChunkSize +
                localZ;

            int index =
                VoxelIndex.ToIndex(
                    localX,
                    localY,
                    localZ
                );

            ushort blockId = 2;
            byte light = 0xA5;
            byte state = 7;

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                localX,
                localY,
                localZ,
                new VoxelData(
                    blockId,
                    light,
                    state
                )
            );

            bool changed =
                world.TrySetVoxel(
                    worldX,
                    worldY,
                    worldZ,
                    3
                );

            Assert.That(
                changed,
                Is.True
            );

            VoxelData beforeUnload =
                chunk.Data.Voxels[index];

            Assert.That(
                beforeUnload.BlockId,
                Is.EqualTo(3)
            );

            Assert.That(
                chunk.NeedsSave,
                Is.True
            );

            bool unloaded =
                world.UnloadChunk(
                    coordinate
                );

            Assert.That(
                unloaded,
                Is.True
            );

            Assert.That(
                world.LoadedChunkCount,
                Is.EqualTo(0)
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

            VoxelData afterReload =
                reloaded.Data.Voxels[index];

            Assert.That(
                afterReload.BlockId,
                Is.EqualTo(3)
            );
        }

        [Test]
        public void ModifiedChunk_PreservesLightAndStateAfterReload()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    6000,
                    0,
                    6000
                );

            Chunk chunk =
                world.LoadAndGenerateChunk(
                    coordinate
                );

            world.CompleteGeneration();

            int localX = 5;
            int localY = 3;
            int localZ = 6;

            int worldX =
                coordinate.X * VoxelConstants.ChunkSize +
                localX;

            int worldY =
                coordinate.Y * VoxelConstants.ChunkSize +
                localY;

            int worldZ =
                coordinate.Z * VoxelConstants.ChunkSize +
                localZ;

            int index =
                VoxelIndex.ToIndex(
                    localX,
                    localY,
                    localZ
                );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                localX,
                localY,
                localZ,
                new VoxelData(
                    1,
                    0xA5,
                    9
                )
            );

            bool changed =
                world.TrySetVoxel(
                    worldX,
                    worldY,
                    worldZ,
                    2
                );

            Assert.That(
                changed,
                Is.True
            );

            VoxelData modified =
                chunk.Data.Voxels[index];

            Assert.That(
                modified.BlockId,
                Is.EqualTo(2)
            );

            Assert.That(
                modified.Light,
                Is.EqualTo(0xA5)
            );

            Assert.That(
                modified.State,
                Is.EqualTo(9)
            );

            world.UnloadChunk(coordinate);

            Chunk reloaded =
                world.LoadChunk(
                    coordinate
                );

            VoxelData restored =
                reloaded.Data.Voxels[index];

            Assert.That(
                restored.BlockId,
                Is.EqualTo(2)
            );

            Assert.That(
                restored.Light,
                Is.EqualTo(0xA5)
            );

            Assert.That(
                restored.State,
                Is.EqualTo(9)
            );
        }

        [Test]
        public void UnmodifiedChunk_DoesNotRequireSave()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    7000,
                    0,
                    7000
                );

            Chunk chunk =
                world.LoadAndGenerateChunk(
                    coordinate
                );

            world.CompleteGeneration();

            Assert.That(
                chunk.NeedsSave,
                Is.False
            );
        }
    }
}
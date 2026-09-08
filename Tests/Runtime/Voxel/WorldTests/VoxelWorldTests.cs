using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WildEarth.Voxel;

using VoxelData = WildEarth.Voxel.Voxel;

namespace WildEarth.Tests.Voxel
{
public sealed class VoxelWorldTests
    {
    private const ushort WaterBlockId = 7;
    private const byte WaterState = 15;


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

            Assert.That(
                blockRegistry,
                Is.Not.Null,
                "No se encontró BlockRegistry.asset."
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

        [Test]
        public void GeneratedChunks_CompleteWithoutActiveJobs()
        {
            GenerateTestChunks();

            Assert.That(
                world.Generator.ActiveJobCount,
                Is.EqualTo(0),
                "La generación de los chunks de prueba no terminó."
            );
        }

        [Test]
        public void GeneratedChunks_ContainExpectedTerrainBelowSeaLevel()
        {
            GenerateTestChunks();

            const int searchRadius = 1;
            const int minChunkY = 0;
            const int maxChunkY = 2;

            int minimumWorldHeight =
                int.MaxValue;

            int maximumWorldHeight =
                int.MinValue;

            ChunkCoordinate minimumChunk =
                default;

            ChunkCoordinate maximumChunk =
                default;

            int minimumColumnX = 0;
            int minimumColumnZ = 0;

            int maximumColumnX = 0;
            int maximumColumnZ = 0;

            for (int y = minChunkY;
                y <= maxChunkY;
                y++)
            {
                for (int z = -searchRadius;
                    z <= searchRadius;
                    z++)
                {
                    for (int x = -searchRadius;
                        x <= searchRadius;
                        x++)
                    {
                        ChunkCoordinate coordinate =
                            new ChunkCoordinate(
                                x,
                                y,
                                z
                            );

                        if (!world.TryGetChunk(
                                coordinate,
                                out Chunk chunk))
                        {
                            continue;
                        }

                        int chunkSize =
                            VoxelConstants.ChunkSize;

                        for (int localZ = 0;
                            localZ < chunkSize;
                            localZ++)
                        {
                            for (int localX = 0;
                                localX < chunkSize;
                                localX++)
                            {
                                int highestSolidLocalY =
                                    FindHighestSolidVoxel(
                                        chunk,
                                        localX,
                                        localZ
                                    );

                                if (highestSolidLocalY < 0)
                                    continue;

                                int worldY =
                                    coordinate.Y *
                                    chunkSize +
                                    highestSolidLocalY;

                                if (worldY <
                                    minimumWorldHeight)
                                {
                                    minimumWorldHeight =
                                        worldY;

                                    minimumChunk =
                                        coordinate;

                                    minimumColumnX =
                                        localX;

                                    minimumColumnZ =
                                        localZ;
                                }

                                if (worldY >
                                    maximumWorldHeight)
                                {
                                    maximumWorldHeight =
                                        worldY;

                                    maximumChunk =
                                        coordinate;

                                    maximumColumnX =
                                        localX;

                                    maximumColumnZ =
                                        localZ;
                                }
                            }
                        }
                    }
                }
            }

            Debug.Log(
                "[Terrain Debug] World height range: " +
                $"{minimumWorldHeight}.." +
                $"{maximumWorldHeight}. " +
                $"Minimum at chunk {minimumChunk}, " +
                $"local=({minimumColumnX}, {minimumColumnZ}). " +
                $"Maximum at chunk {maximumChunk}, " +
                $"local=({maximumColumnX}, {maximumColumnZ}). " +
                $"SeaLevel=" +
                $"{ChunkGenerationSettings.Default.Terrain.SeaLevel}"
            );

            Assert.That(
                minimumWorldHeight,
                Is.LessThan(
                    ChunkGenerationSettings.Default.Terrain.SeaLevel
                ),
                "No se encontró ninguna columna de terreno por debajo del SeaLevel."
            );
        }

        [Test]
        public void GeneratedChunks_ContainWaterWhenTerrainIsBelowSeaLevel()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(-8, 2, -3);

            Chunk chunk =
                world.LoadAndGenerateChunk(coordinate);

            world.CompleteGeneration();

            int waterVoxelCount = 0;

            for (int i = 0;
                i < chunk.Data.Voxels.Length;
                i++)
            {
                VoxelData voxel =
                    chunk.Data.Voxels[i];

                if (voxel.BlockId == WaterBlockId)
                {
                    waterVoxelCount++;
                }
            }

            Debug.Log(
                "[Fluid Debug] Generated water voxel count: " +
                $"{waterVoxelCount}. " +
                $"Chunk={coordinate}"
            );

            Assert.That(
                waterVoxelCount,
                Is.GreaterThan(0),
                "La generación produjo terreno bajo SeaLevel, " +
                "pero ningún voxel de agua."
            );
        }

        [Test]
        public void GeneratedChunkWithFluid_StartsFluidSimulation()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(-8, 2, -3);

            Chunk chunk =
                world.LoadAndGenerateChunk(coordinate);

            world.CompleteGeneration();

            Assert.That(
                ChunkContainsWater(chunk),
                Is.True,
                "El chunk generado no contiene agua."
            );

            world.Update();

            Assert.That(
                world.FluidSimulation.RunningCount,
                Is.GreaterThanOrEqualTo(1),
                "Se encontró agua, pero VoxelWorld no inició ninguna simulación de fluidos."
            );
        }

        [Test]
        public void DebugWaterGenerationConfiguration()
        {
            FluidRuntimeData water =
                world.Fluids.GetRuntimeData(
                    FluidType.Water
                );

            Debug.Log(
                "[Fluid Debug] Water runtime: " +
                $"Type={water.Type}, " +
                $"BlockId={water.BlockId}, " +
                $"MaxLevel={water.MaxLevel}, " +
                $"HorizontalDecay={water.HorizontalFlowDecay}, " +
                $"VerticalDecay={water.VerticalFlowDecay}, " +
                $"IsLava={water.IsLava}, " +
                $"IsValid={water.IsValid}"
            );

            Assert.That(
                water.IsValid,
                Is.True,
                "El runtime de Water no es válido."
            );

            Assert.That(
                water.BlockId,
                Is.EqualTo(WaterBlockId),
                "El BlockId runtime de Water no coincide con el esperado."
            );

            Assert.That(
                water.MaxLevel,
                Is.EqualTo(WaterState),
                "El MaxLevel runtime de Water no coincide con el esperado."
            );
        }

        private void GenerateTestChunks()
        {
            const int searchRadius = 1;
            const int minChunkY = 0;
            const int maxChunkY = 2;

            for (int y = minChunkY;
                y <= maxChunkY;
                y++)
            {
                for (int z = -searchRadius;
                    z <= searchRadius;
                    z++)
                {
                    for (int x = -searchRadius;
                        x <= searchRadius;
                        x++)
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

            world.CompleteGeneration();
        }

        private static int FindHighestSolidVoxel(
            Chunk chunk,
            int localX,
            int localZ)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            for (int localY = chunkSize - 1;
                localY >= 0;
                localY--)
            {
                VoxelData voxel =
                    ChunkDataAccess.GetVoxel(
                        chunk.Data,
                        localX,
                        localY,
                        localZ
                    );

                if (!voxel.IsAir)
                    return localY;
            }

            return -1;
        }

        private static bool ChunkContainsWater(
            Chunk chunk)
        {
            if (chunk == null ||
                !chunk.Data.IsCreated)
            {
                return false;
            }

            int chunkSize =
                VoxelConstants.ChunkSize;

            for (int z = 0;
                z < chunkSize;
                z++)
            {
                for (int y = 0;
                    y < chunkSize;
                    y++)
                {
                    for (int x = 0;
                        x < chunkSize;
                        x++)
                    {
                        VoxelData voxel =
                            ChunkDataAccess.GetVoxel(
                                chunk.Data,
                                x,
                                y,
                                z
                            );

                        if (voxel.BlockId ==
                                WaterBlockId &&
                            voxel.State ==
                                WaterState)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }

}
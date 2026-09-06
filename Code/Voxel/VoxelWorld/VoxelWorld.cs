using System;
using Unity.Collections;
using UnityEngine;

namespace WildEarth.Voxel
{
    public sealed class VoxelWorld : IDisposable
    {
        private readonly ChunkStorage chunkStorage;
        private readonly ChunkGenerator chunkGenerator;

        private readonly BiomeRegistry biomeRegistry;
        private readonly BiomeRuntimeDatabase biomeDatabase;

        private readonly BlockRegistry blockRegistry;
        private readonly BlockRuntimeDatabase blockDatabase;

        private readonly OreRegistry oreRegistry;
        private readonly OreRuntimeDatabase oreDatabase;

        private readonly FluidRegistry fluidRegistry;
        private readonly FluidRuntimeDatabase fluidDatabase;

        private readonly FluidScheduler fluidScheduler;
        private readonly FluidSimulationCoordinator
            fluidSimulationCoordinator;

        private readonly FluidSimulationSettings
            fluidSimulationSettings;

        private bool initialized;
        private bool disposed;

        public bool IsInitialized =>
            initialized;

        public int LoadedChunkCount =>
            chunkStorage?.Count ?? 0;

        public ChunkStorage Chunks =>
            chunkStorage;

        public ChunkGenerator Generator =>
            chunkGenerator;

        public BiomeRegistry Biomes =>
            biomeRegistry;

        public BlockRegistry Blocks =>
            blockRegistry;

        public OreRegistry Ores =>
            oreRegistry;

        public FluidRegistry Fluids =>
            fluidRegistry;

        public FluidSimulationCoordinator FluidSimulation =>
            fluidSimulationCoordinator;

        public FluidScheduler FluidScheduler =>
            fluidScheduler;

        public VoxelWorld(
            VoxelWorldSettings worldSettings,
            ChunkGenerationSettings generationSettings,
            BiomeRegistryAsset biomeRegistryAsset,
            BlockRegistry blockRegistry,
            OreRegistryAsset oreRegistryAsset,
            FluidRegistryAsset fluidRegistryAsset)
        {
            if (oreRegistryAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(oreRegistryAsset)
                );
            }

            if (fluidRegistryAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(fluidRegistryAsset)
                );
            }

            if (biomeRegistryAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(biomeRegistryAsset)
                );
            }

            if (blockRegistry == null)
            {
                throw new ArgumentNullException(
                    nameof(blockRegistry)
                );
            }

            this.blockRegistry =
                blockRegistry;

            biomeRegistry =
                new BiomeRegistry(
                    biomeRegistryAsset
                );

            biomeDatabase =
                new BiomeRuntimeDatabase(
                    biomeRegistry,
                    Allocator.Persistent
                );

            blockDatabase =
                new BlockRuntimeDatabase(
                    this.blockRegistry,
                    Allocator.Persistent
                );

            oreRegistry =
                new OreRegistry(
                    oreRegistryAsset
                );

            oreDatabase =
                new OreRuntimeDatabase(
                    oreRegistry,
                    Allocator.Persistent
                );

            fluidRegistry =
                new FluidRegistry(
                    fluidRegistryAsset
                );

            fluidDatabase =
                new FluidRuntimeDatabase(
                    fluidRegistry,
                    Allocator.Persistent
                );

            fluidSimulationSettings =
                FluidSimulationSettings.Default;

            ChunkDataPool chunkDataPool =
                new ChunkDataPool(
                    Allocator.Persistent,
                    worldSettings.InitialChunkPoolSize,
                    worldSettings.MaximumChunkPoolSize
                );

            ChunkBiomeDataPool chunkBiomeDataPool =
                new ChunkBiomeDataPool(
                    Allocator.Persistent,
                    worldSettings.InitialChunkPoolSize,
                    worldSettings.MaximumChunkPoolSize
                );

            chunkStorage =
                new ChunkStorage(
                    chunkDataPool,
                    chunkBiomeDataPool,
                    worldSettings.InitialChunkStorageCapacity
                );

            chunkGenerator =
                new ChunkGenerator(
                    generationSettings,
                    biomeDatabase,
                    blockDatabase,
                    oreDatabase,
                    fluidDatabase
                );

            FluidUpdateSystem fluidUpdateSystem =
                new FluidUpdateSystem(
                    chunkStorage,
                    fluidDatabase,
                    fluidSimulationSettings
                );

            fluidScheduler =
                new FluidScheduler(
                    fluidUpdateSystem,
                    fluidSimulationSettings
                );

            fluidSimulationCoordinator =
                new FluidSimulationCoordinator(
                    chunkStorage,
                    fluidScheduler,
                    FluidSimulationCoordinatorSettings.Default
                );
        }

        public void Initialize()
        {
            ThrowIfDisposed();

            if (initialized)
                return;

            initialized = true;
        }

        public void Update()
        {
            ThrowIfNotInitialized();

            chunkGenerator.Update();

            ProcessCompletedChunks();

            fluidSimulationCoordinator
                .CompleteFinishedAndSchedulePending(
                    fluidDatabase,
                    fluidSimulationSettings
                );

            fluidScheduler.Advance(
                Time.deltaTime
            );
        }

        public void CompleteGeneration()
        {
            ThrowIfNotInitialized();

            chunkGenerator.CompleteAll();
        }

        public Chunk LoadChunk(
            ChunkCoordinate coordinate)
        {
            ThrowIfNotInitialized();

            if (chunkStorage.TryGet(
                    coordinate,
                    out Chunk existingChunk))
            {
                return existingChunk;
            }

            return chunkStorage.Create(
                coordinate
            );
        }

        public Chunk LoadAndGenerateChunk(
            ChunkCoordinate coordinate)
        {
            Chunk chunk =
                LoadChunk(coordinate);

            if (chunk.State ==
                ChunkState.Loading)
            {
                chunkGenerator.Schedule(
                    chunk
                );
            }

            return chunk;
        }

        public bool UnloadChunk(
            ChunkCoordinate coordinate)
        {
            ThrowIfNotInitialized();

            if (!chunkStorage.TryGet(
                    coordinate,
                    out Chunk chunk))
            {
                return false;
            }

            chunkGenerator.CompleteChunk(
                chunk
            );

            fluidSimulationCoordinator.Remove(
                coordinate
            );

            return chunkStorage.Remove(
                coordinate
            );
        }

        public bool TryGetChunk(
            ChunkCoordinate coordinate,
            out Chunk chunk)
        {
            ThrowIfNotInitialized();

            return chunkStorage.TryGet(
                coordinate,
                out chunk
            );
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            fluidSimulationCoordinator.Dispose();

            chunkGenerator.Dispose();

            chunkStorage.Dispose();

            biomeDatabase.Dispose();

            blockDatabase.Dispose();

            oreDatabase.Dispose();

            fluidDatabase.Dispose();

            initialized = false;
        }

        private void ProcessCompletedChunks()
        {
            var completedChunks =
                chunkGenerator.CompletedChunks;

            for (int i = 0;
                 i < completedChunks.Count;
                 i++)
            {
                Chunk chunk =
                    completedChunks[i];

                fluidSimulationCoordinator
                    .RequestChunkSimulation(
                        chunk.Coordinate,
                        fluidDatabase
                    );
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(
                    nameof(VoxelWorld)
                );
            }
        }

        private void ThrowIfNotInitialized()
        {
            ThrowIfDisposed();

            if (!initialized)
            {
                throw new InvalidOperationException(
                    "VoxelWorld todavía no fue inicializado."
                );
            }
        }
    }
}
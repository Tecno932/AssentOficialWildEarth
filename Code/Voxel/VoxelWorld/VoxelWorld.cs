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

        public BlockRuntimeDatabase BlockDatabase =>
            blockDatabase;

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

            ProcessCompletedChunks();
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

                if (chunk == null)
                    continue;

                fluidSimulationCoordinator
                    .RequestChunkSimulation(
                        chunk.Coordinate,
                        fluidDatabase
                    );

                MarkNeighborChunksForRemesh(
                    chunk
                );
            }
        }

        private void MarkNeighborChunksForRemesh(
            Chunk completedChunk)
        {
            ChunkCoordinate coordinate =
                completedChunk.Coordinate;

            TryMarkNeighborForRemesh(
                new ChunkCoordinate(
                    coordinate.X - 1,
                    coordinate.Y,
                    coordinate.Z
                )
            );

            TryMarkNeighborForRemesh(
                new ChunkCoordinate(
                    coordinate.X + 1,
                    coordinate.Y,
                    coordinate.Z
                )
            );

            TryMarkNeighborForRemesh(
                new ChunkCoordinate(
                    coordinate.X,
                    coordinate.Y - 1,
                    coordinate.Z
                )
            );

            TryMarkNeighborForRemesh(
                new ChunkCoordinate(
                    coordinate.X,
                    coordinate.Y + 1,
                    coordinate.Z
                )
            );

            TryMarkNeighborForRemesh(
                new ChunkCoordinate(
                    coordinate.X,
                    coordinate.Y,
                    coordinate.Z - 1
                )
            );

            TryMarkNeighborForRemesh(
                new ChunkCoordinate(
                    coordinate.X,
                    coordinate.Y,
                    coordinate.Z + 1
                )
            );
        }

        private void TryMarkNeighborForRemesh(
            ChunkCoordinate coordinate)
        {
            if (!chunkStorage.TryGet(
                    coordinate,
                    out Chunk neighbor))
            {
                return;
            }

            if (neighbor == null)
                return;

            if (neighbor.State != ChunkState.Generated &&
                neighbor.State != ChunkState.Ready)
            {
                return;
            }

            neighbor.MarkNeedsMesh();
        }

        public void DebugLogChunkBlockCounts(
            ChunkCoordinate coordinate)
        {
            ThrowIfNotInitialized();

            if (!chunkStorage.TryGet(
                    coordinate,
                    out Chunk chunk))
            {
                Debug.Log(
                    $"[VoxelDebug] Chunk {coordinate} no existe."
                );

                return;
            }

            if (chunk == null ||
                !chunk.Data.IsCreated)
            {
                Debug.Log(
                    $"[VoxelDebug] Chunk {coordinate} no tiene datos."
                );

                return;
            }

            int air = 0;
            int stone = 0;
            int dirt = 0;
            int grass = 0;
            int other = 0;

            NativeArray<Voxel> voxels =
                chunk.Data.Voxels;

            for (int i = 0; i < voxels.Length; i++)
            {
                switch (voxels[i].BlockId)
                {
                    case 0:
                        air++;
                        break;

                    case 1:
                        stone++;
                        break;

                    case 2:
                        dirt++;
                        break;

                    case 3:
                        grass++;
                        break;

                    default:
                        other++;
                        break;
                }
            }

            Debug.Log(
                $"[VoxelDebug] Chunk {coordinate} | " +
                $"State={chunk.State} | " +
                $"Air={air} | " +
                $"Stone={stone} | " +
                $"Dirt={dirt} | " +
                $"Grass={grass} | " +
                $"Other={other}"
            );
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
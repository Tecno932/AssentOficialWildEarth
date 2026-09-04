using System;
using Unity.Collections;

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

        public VoxelWorld(
            VoxelWorldSettings worldSettings,
            ChunkGenerationSettings generationSettings,
            BiomeRegistryAsset biomeRegistryAsset,
            BlockRegistry blockRegistry,
            OreRegistryAsset oreRegistryAsset)
        {
            if (oreRegistryAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(oreRegistryAsset)
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
                    oreDatabase
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

            chunkGenerator.Dispose();

            chunkStorage.Dispose();

            biomeDatabase.Dispose();

            blockDatabase.Dispose();

            oreDatabase.Dispose();

            initialized = false;
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
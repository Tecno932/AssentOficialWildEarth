using System;
using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;

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
        private readonly FluidSimulationCoordinator fluidSimulationCoordinator;
        private readonly FluidSimulationSettings fluidSimulationSettings;
        private readonly ChunkSaveStorage saveStorage;

        private bool initialized;
        private bool disposed;

        public bool IsInitialized => initialized;
        public int LoadedChunkCount => chunkStorage?.Count ?? 0;
        public ChunkStorage Chunks => chunkStorage;
        public ChunkGenerator Generator => chunkGenerator;
        public BiomeRegistry Biomes => biomeRegistry;
        public BlockRegistry Blocks => blockRegistry;
        public BlockRuntimeDatabase BlockDatabase => blockDatabase;
        public OreRegistry Ores => oreRegistry;
        public FluidRegistry Fluids => fluidRegistry;
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
            FluidRegistryAsset fluidRegistryAsset,
            string savePath = null)
        {
            if (oreRegistryAsset == null)
                throw new ArgumentNullException(
                    nameof(oreRegistryAsset)
                );

            if (fluidRegistryAsset == null)
                throw new ArgumentNullException(
                    nameof(fluidRegistryAsset)
                );

            if (biomeRegistryAsset == null)
                throw new ArgumentNullException(
                    nameof(biomeRegistryAsset)
                );

            if (blockRegistry == null)
                throw new ArgumentNullException(
                    nameof(blockRegistry)
                );

            this.blockRegistry = blockRegistry;

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

            if (string.IsNullOrWhiteSpace(savePath))
            {
                savePath =
                    System.IO.Path.Combine(
                        Application.persistentDataPath,
                        "World"
                    );
            }

            saveStorage =
                new ChunkSaveStorage(
                    savePath
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

            fluidSimulationCoordinator
                .CompleteAllBeforeWrites();

            fluidScheduler.Advance(
                Time.deltaTime
            );

            while (
                fluidScheduler.TryConsumeChangedChunk(
                    out ChunkCoordinate changedCoordinate))
            {
                fluidSimulationCoordinator.RequestChunkSimulation(
                    changedCoordinate,
                    fluidDatabase
                );
            }
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

            Chunk chunk =
                chunkStorage.Create(
                    coordinate
                );

            if (saveStorage.TryLoad(
                coordinate,
                chunk.Data.Voxels))
            {
                chunk.MarkGenerated();
                chunk.SetLoadedFromDisk();
                chunk.MarkNeedsMesh();

                int air = 0;
                int solid = 0;
                int fluid = 0;

                for (int i = 0; i < chunk.Data.Voxels.Length; i++)
                {
                    Voxel voxel = chunk.Data.Voxels[i];

                    if (voxel.BlockId == BlockIds.Air)
                    {
                        air++;
                        continue;
                    }

                    if (blockDatabase.TryGet(
                            voxel.BlockId,
                            out BlockRuntimeData block))
                    {
                        if (block.IsFluid &&
                            voxel.State > 0)
                        {
                            fluid++;
                        }
                        else
                        {
                            solid++;
                        }
                    }
                    else
                    {
                        solid++;
                    }
                }

                Debug.Log(
                    $"[VoxelLoad] Chunk cargado desde disco: {coordinate} | " +
                    $"Air={air} | Solid={solid} | Fluid={fluid} | " +
                    $"State={chunk.State} | NeedsMesh={chunk.NeedsMesh}"
                );

                MarkNeighborChunksForRemesh(
                    chunk
                );
            }

            return chunk;
        }

        public Chunk LoadAndGenerateChunk(
            ChunkCoordinate coordinate)
        {
            Chunk chunk =
                LoadChunk(
                    coordinate
                );

            if (chunk.State == ChunkState.Loading)
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

            SaveChunkIfNeeded(
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

            if (initialized)
            {
                SaveModifiedChunks();
            }

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

        private void SaveModifiedChunks()
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

                if (chunk.NeedsSave)
                {
                    SaveChunkIfNeeded(
                        chunk
                    );
                }
            }

            var coordinates =
                new System.Collections.Generic.List<ChunkCoordinate>();

            chunkStorage.GetCoordinates(
                coordinates
            );

            for (int i = 0;
                i < coordinates.Count;
                i++)
            {
                if (!chunkStorage.TryGet(
                        coordinates[i],
                        out Chunk chunk))
                {
                    continue;
                }

                if (chunk == null)
                    continue;

                SaveChunkIfNeeded(
                    chunk
                );
            }
        }

        private void SaveChunkIfNeeded(
            Chunk chunk)
        {
            if (chunk == null)
                return;

            if (!chunk.NeedsSave)
                return;

            if (!chunk.Data.IsCreated)
                return;

            saveStorage.Save(
                chunk.Coordinate,
                chunk.Data.Voxels
            );

            chunk.MarkSaved();

            Debug.Log(
                $"[VoxelSave] Chunk guardado: {chunk.Coordinate}"
            );
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

        private void MarkNeighborForRemesh(
            int x,
            int y,
            int z)
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    x,
                    y,
                    z
                );

            if (!chunkStorage.TryGet(
                    coordinate,
                    out Chunk neighbor))
            {
                return;
            }

            if (neighbor == null)
            {
                return;
            }

            if (!neighbor.Data.IsCreated)
            {
                return;
            }

            neighbor.MarkNeedsMesh();
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

        public bool TrySetVoxel(
            int worldX,
            int worldY,
            int worldZ,
            ushort blockId)
        {
            ThrowIfNotInitialized();

            if (worldY < VoxelConstants.MinVoxelY ||
                worldY > VoxelConstants.MaxVoxelY)
            {
                return false;
            }

            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    FloorDiv(
                        worldX,
                        VoxelConstants.ChunkSize
                    ),
                    FloorDiv(
                        worldY,
                        VoxelConstants.ChunkSize
                    ),
                    FloorDiv(
                        worldZ,
                        VoxelConstants.ChunkSize
                    )
                );

            if (!chunkStorage.TryGet(
                    coordinate,
                    out Chunk chunk))
            {
                return false;
            }

            if (chunk == null ||
                !chunk.Data.IsCreated)
            {
                return false;
            }

            if (chunk.State != ChunkState.Generated &&
                chunk.State != ChunkState.Ready)
            {
                return false;
            }

            int localX =
                Mod(
                    worldX,
                    VoxelConstants.ChunkSize
                );

            int localY =
                Mod(
                    worldY,
                    VoxelConstants.ChunkSize
                );

            int localZ =
                Mod(
                    worldZ,
                    VoxelConstants.ChunkSize
                );

            int index =
                VoxelIndex.ToIndex(
                    localX,
                    localY,
                    localZ
                );

            Voxel current =
                chunk.Data.Voxels[index];

            if (current.BlockId == blockId)
                return false;

            NativeArray<Voxel> voxels =
                chunk.Data.Voxels;

            voxels[index] =
                new Voxel(
                    blockId,
                    current.Light,
                    current.State
                );

            chunk.MarkVoxelDataChanged();

            fluidSimulationCoordinator.RequestChunkSimulation(
                coordinate,
                fluidDatabase
            );

            if (localX == 0)
            {
                MarkNeighborForRemesh(
                    coordinate.X - 1,
                    coordinate.Y,
                    coordinate.Z
                );
            }
            else if (localX == VoxelConstants.ChunkSize - 1)
            {
                MarkNeighborForRemesh(
                    coordinate.X + 1,
                    coordinate.Y,
                    coordinate.Z
                );
            }

            if (localY == 0)
            {
                MarkNeighborForRemesh(
                    coordinate.X,
                    coordinate.Y - 1,
                    coordinate.Z
                );
            }
            else if (localY == VoxelConstants.ChunkSize - 1)
            {
                MarkNeighborForRemesh(
                    coordinate.X,
                    coordinate.Y + 1,
                    coordinate.Z
                );
            }

            if (localZ == 0)
            {
                MarkNeighborForRemesh(
                    coordinate.X,
                    coordinate.Y,
                    coordinate.Z - 1
                );
            }
            else if (localZ == VoxelConstants.ChunkSize - 1)
            {
                MarkNeighborForRemesh(
                    coordinate.X,
                    coordinate.Y,
                    coordinate.Z + 1
                );
            }

            return true;
        }

        public bool TryGetBiomeAt(
            int worldX,
            int worldZ,
            out BiomeId biomeId)
        {
            biomeId = BiomeId.Plains;

            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    FloorDiv(
                        worldX,
                        VoxelConstants.ChunkSize
                    ),
                    0,
                    FloorDiv(
                        worldZ,
                        VoxelConstants.ChunkSize
                    )
                );

            if (!chunkStorage.TryGet(
                    coordinate,
                    out Chunk chunk))
            {
                return false;
            }

            if (chunk == null ||
                !chunk.BiomeData.IsCreated)
            {
                return false;
            }

            if (chunk.State != ChunkState.Generated &&
                chunk.State != ChunkState.Ready)
            {
                return false;
            }

            int localX =
                Mod(
                    worldX,
                    VoxelConstants.ChunkSize
                );

            int localZ =
                Mod(
                    worldZ,
                    VoxelConstants.ChunkSize
                );

            int index =
                localX +
                localZ * VoxelConstants.ChunkSize;

            biomeId =
                chunk.BiomeData.Biomes[index];

            return true;
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

            for (int i = 0;
                i < voxels.Length;
                i++)
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
                $"Air={air} | Stone={stone} | " +
                $"Dirt={dirt} | Grass={grass} | " +
                $"Other={other}"
            );
        }

        private static int FloorDiv(
            int value,
            int divisor)
        {
            int result =
                value / divisor;

            int remainder =
                value % divisor;

            if (remainder != 0 &&
                ((remainder < 0) !=
                 (divisor < 0)))
            {
                result--;
            }

            return result;
        }

        private static int Mod(
            int value,
            int modulus)
        {
            int result =
                value % modulus;

            if (result < 0)
                result += modulus;

            return result;
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
using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace WildEarth.Voxel
{
    public sealed class ChunkGenerator : IDisposable, IChunkGenerator
    {
        private readonly ChunkGenerationSettings settings;
        private readonly ChunkGenerationPipeline pipeline;
        private readonly BiomeRuntimeDatabase biomeDatabase;
        private readonly BlockRuntimeDatabase blockDatabase;
        private readonly OreRuntimeDatabase oreDatabase;
        private readonly FluidRuntimeDatabase fluidDatabase;

        private readonly List<GenerationTask> activeTasks =
            new List<GenerationTask>();

        private readonly List<Chunk> completedChunks =
            new List<Chunk>();

        private bool disposed;

        public int ActiveJobCount =>
            activeTasks.Count;

        public IReadOnlyList<Chunk> CompletedChunks =>
            completedChunks;

        public ChunkGenerator(
            ChunkGenerationSettings settings,
            BiomeRuntimeDatabase biomeDatabase,
            BlockRuntimeDatabase blockDatabase,
            OreRuntimeDatabase oreDatabase,
            FluidRuntimeDatabase fluidDatabase)
        {
            this.settings = settings;

            this.biomeDatabase =
                biomeDatabase ??
                throw new ArgumentNullException(
                    nameof(biomeDatabase)
                );

            this.blockDatabase =
                blockDatabase ??
                throw new ArgumentNullException(
                    nameof(blockDatabase)
                );

            this.oreDatabase =
                oreDatabase ??
                throw new ArgumentNullException(
                    nameof(oreDatabase)
                );

            pipeline =
                new ChunkGenerationPipeline(
                    settings,
                    biomeDatabase,
                    blockDatabase,
                    oreDatabase,
                    fluidDatabase
                );
        }

        public JobHandle Schedule(
            Chunk chunk,
            JobHandle dependency = default)
        {
            ThrowIfDisposed();

            if (chunk == null)
            {
                throw new ArgumentNullException(
                    nameof(chunk)
                );
            }

            if (!chunk.Data.IsCreated)
            {
                throw new InvalidOperationException(
                    $"El chunk {chunk.Coordinate} no tiene datos válidos."
                );
            }

            if (IsGenerating(chunk))
            {
                throw new InvalidOperationException(
                    $"El chunk {chunk.Coordinate} ya tiene una generación activa."
                );
            }

            chunk.SetState(
                ChunkState.Generating
            );

            JobHandle handle =
                pipeline.Schedule(
                    chunk,
                    dependency
                );

            activeTasks.Add(
                new GenerationTask(
                    chunk,
                    handle
                )
            );

            return handle;
        }

        public bool IsGenerating(
            Chunk chunk)
        {
            if (chunk == null)
                return false;

            for (int i = 0; i < activeTasks.Count; i++)
            {
                if (ReferenceEquals(
                        activeTasks[i].Chunk,
                        chunk))
                {
                    return true;
                }
            }

            return false;
        }

        public void CompleteChunk(
            Chunk chunk)
        {
            ThrowIfDisposed();

            if (chunk == null)
                return;

            for (int i = activeTasks.Count - 1; i >= 0; i--)
            {
                GenerationTask task =
                    activeTasks[i];

                if (!ReferenceEquals(
                        task.Chunk,
                        chunk))
                {
                    continue;
                }

                task.Handle.Complete();

                task.Chunk.MarkGenerated();

                completedChunks.Add(
                    task.Chunk
                );

                activeTasks.RemoveAt(i);

                return;
            }
        }

        public void Update()
        {
            ThrowIfDisposed();

            completedChunks.Clear();

            for (int i = activeTasks.Count - 1; i >= 0; i--)
            {
                GenerationTask task =
                    activeTasks[i];

                if (!task.Handle.IsCompleted)
                    continue;

                task.Handle.Complete();

                task.Chunk.MarkGenerated();

                completedChunks.Add(
                    task.Chunk
                );

                activeTasks.RemoveAt(i);
            }
        }

        public void CompleteAll()
        {
            ThrowIfDisposed();

            for (int i = 0; i < activeTasks.Count; i++)
            {
                GenerationTask task =
                    activeTasks[i];

                task.Handle.Complete();

                task.Chunk.MarkGenerated();
            }

            activeTasks.Clear();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            CompleteAll();

            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(
                    nameof(ChunkGenerator)
                );
            }
        }

        private readonly struct GenerationTask
        {
            public readonly Chunk Chunk;
            public readonly JobHandle Handle;

            public GenerationTask(
                Chunk chunk,
                JobHandle handle)
            {
                Chunk = chunk;
                Handle = handle;
            }
        }
    }
}
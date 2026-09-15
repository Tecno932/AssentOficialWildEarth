using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace WildEarth.Voxel
{
    public sealed class ChunkGenerator : IDisposable, IChunkGenerator
    {
        private const int MaxConcurrentGenerationJobs = 8;

        private readonly ChunkGenerationSettings settings;
        private readonly ChunkGenerationPipeline pipeline;
        private readonly BiomeRuntimeDatabase biomeDatabase;
        private readonly BlockRuntimeDatabase blockDatabase;
        private readonly OreRuntimeDatabase oreDatabase;
        private readonly FluidRuntimeDatabase fluidDatabase;

        private readonly List<GenerationTask> activeTasks =
            new List<GenerationTask>();

        private readonly Queue<GenerationRequest> pendingRequests =
            new Queue<GenerationRequest>();

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

            ValidateChunk(chunk);

            if (IsGenerating(chunk))
            {
                throw new InvalidOperationException(
                    $"El chunk {chunk.Coordinate} ya tiene una generación activa."
                );
            }

            if (IsPending(chunk))
            {
                throw new InvalidOperationException(
                    $"El chunk {chunk.Coordinate} ya está en cola de generación."
                );
            }

            if (activeTasks.Count >= MaxConcurrentGenerationJobs)
            {
                chunk.SetState(
                    ChunkState.Generating
                );

                pendingRequests.Enqueue(
                    new GenerationRequest(
                        chunk,
                        dependency
                    )
                );

                return default;
            }

            return ScheduleImmediate(
                chunk,
                dependency
            );
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

                SchedulePendingRequests();

                return;
            }

            RemovePendingRequest(chunk);
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

            SchedulePendingRequests();
        }

        public void CompleteAll()
        {
            ThrowIfDisposed();

            while (activeTasks.Count > 0 ||
                   pendingRequests.Count > 0)
            {
                for (int i = activeTasks.Count - 1; i >= 0; i--)
                {
                    GenerationTask task =
                        activeTasks[i];

                    task.Handle.Complete();

                    task.Chunk.MarkGenerated();

                    completedChunks.Add(
                        task.Chunk
                    );

                    activeTasks.RemoveAt(i);
                }

                SchedulePendingRequests();
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            CompleteAll();

            pendingRequests.Clear();

            disposed = true;
        }

        private JobHandle ScheduleImmediate(
            Chunk chunk,
            JobHandle dependency)
        {
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

        private void SchedulePendingRequests()
        {
            while (
                activeTasks.Count <
                    MaxConcurrentGenerationJobs &&
                pendingRequests.Count > 0)
            {
                GenerationRequest request =
                    pendingRequests.Dequeue();

                if (request.Chunk == null)
                    continue;

                if (!request.Chunk.Data.IsCreated)
                    continue;

                if (IsGenerating(request.Chunk))
                    continue;

                ScheduleImmediate(
                    request.Chunk,
                    request.Dependency
                );
            }
        }

        private bool IsPending(
            Chunk chunk)
        {
            foreach (
                GenerationRequest request
                in pendingRequests)
            {
                if (ReferenceEquals(
                        request.Chunk,
                        chunk))
                {
                    return true;
                }
            }

            return false;
        }

        private void RemovePendingRequest(
            Chunk chunk)
        {
            if (pendingRequests.Count == 0)
                return;

            int count =
                pendingRequests.Count;

            for (int i = 0; i < count; i++)
            {
                GenerationRequest request =
                    pendingRequests.Dequeue();

                if (!ReferenceEquals(
                        request.Chunk,
                        chunk))
                {
                    pendingRequests.Enqueue(
                        request
                    );
                }
            }
        }

        private void ValidateChunk(
            Chunk chunk)
        {
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

        private readonly struct GenerationRequest
        {
            public readonly Chunk Chunk;
            public readonly JobHandle Dependency;

            public GenerationRequest(
                Chunk chunk,
                JobHandle dependency)
            {
                Chunk = chunk;
                Dependency = dependency;
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
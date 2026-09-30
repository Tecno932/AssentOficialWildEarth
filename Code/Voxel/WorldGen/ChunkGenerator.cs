using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace WildEarth.Voxel
{
    public sealed class ChunkGenerator : IDisposable, IChunkGenerator
    {
        private const int MaxConcurrentGenerationJobs = 4;

        private readonly ChunkGenerationSettings settings;
        private readonly ChunkGenerationPipeline pipeline;
        private readonly BiomeRuntimeDatabase biomeDatabase;
        private readonly BlockRuntimeDatabase blockDatabase;
        private readonly OreRuntimeDatabase oreDatabase;
        private readonly FluidRuntimeDatabase fluidDatabase;

        private readonly List<GenerationTask> activeTasks =
            new List<GenerationTask>();

        private readonly HashSet<Chunk> activeChunks =
            new HashSet<Chunk>();

        private readonly List<GenerationRequest> pendingRequests =
            new List<GenerationRequest>();

        private readonly HashSet<Chunk> pendingChunks =
            new HashSet<Chunk>();

        private readonly List<Chunk> completedChunks =
            new List<Chunk>();

        private ChunkCoordinate generationPriorityCenter;

        private bool hasGenerationPriorityCenter;

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

        public void SetGenerationPriorityCenter(
            ChunkCoordinate center)
        {
            ThrowIfDisposed();

            generationPriorityCenter =
                center;

            hasGenerationPriorityCenter =
                true;
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

            if (pendingChunks.Contains(chunk))
            {
                throw new InvalidOperationException(
                    $"El chunk {chunk.Coordinate} ya está en cola de generación."
                );
            }

            chunk.SetState(
                ChunkState.Generating
            );

            pendingRequests.Add(
                new GenerationRequest(
                    chunk,
                    dependency
                )
            );

            pendingChunks.Add(chunk);

            /*
             * La generación se inicia desde Update().
             *
             * Esto permite que VoxelWorldRuntime pueda encolar
             * toda la zona primero y que después los trabajos
             * se seleccionen realmente por prioridad espacial.
             */
            return default;
        }

        public bool IsGenerating(
            Chunk chunk)
        {
            if (chunk == null)
                return false;

            return activeChunks.Contains(chunk);
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

                activeChunks.Remove(
                    task.Chunk
                );

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

                activeChunks.Remove(
                    task.Chunk
                );
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

                    activeChunks.Remove(
                        task.Chunk
                    );
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
            pendingChunks.Clear();
            activeChunks.Clear();

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

            activeChunks.Add(chunk);

            return handle;
        }

        private void SchedulePendingRequests()
        {
            while (
                activeTasks.Count <
                    MaxConcurrentGenerationJobs &&
                pendingRequests.Count > 0)
            {
                int bestIndex =
                    FindBestPendingRequestIndex();

                GenerationRequest request =
                    pendingRequests[bestIndex];

                pendingRequests.RemoveAt(
                    bestIndex
                );

                pendingChunks.Remove(
                    request.Chunk
                );

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

        private int FindBestPendingRequestIndex()
        {
            int bestIndex = 0;

            GenerationRequest bestRequest =
                pendingRequests[0];

            for (int i = 1;
                 i < pendingRequests.Count;
                 i++)
            {
                GenerationRequest candidate =
                    pendingRequests[i];

                if (CompareGenerationPriority(
                        candidate.Chunk,
                        bestRequest.Chunk) < 0)
                {
                    bestIndex = i;
                    bestRequest = candidate;
                }
            }

            return bestIndex;
        }

        private int CompareGenerationPriority(
            Chunk a,
            Chunk b)
        {
            if (a == null)
                return 1;

            if (b == null)
                return -1;

            if (!hasGenerationPriorityCenter)
                return 0;

            ChunkCoordinate center =
                generationPriorityCenter;

            ChunkCoordinate coordinateA =
                a.Coordinate;

            ChunkCoordinate coordinateB =
                b.Coordinate;

            int ringA =
                Math.Max(
                    Math.Abs(
                        coordinateA.X - center.X
                    ),
                    Math.Abs(
                        coordinateA.Z - center.Z
                    )
                );

            int ringB =
                Math.Max(
                    Math.Abs(
                        coordinateB.X - center.X
                    ),
                    Math.Abs(
                        coordinateB.Z - center.Z
                    )
                );

            if (ringA != ringB)
            {
                return ringA.CompareTo(
                    ringB
                );
            }

            int verticalDistanceA =
                Math.Abs(
                    coordinateA.Y - center.Y
                );

            int verticalDistanceB =
                Math.Abs(
                    coordinateB.Y - center.Y
                );

            if (verticalDistanceA !=
                verticalDistanceB)
            {
                return verticalDistanceA.CompareTo(
                    verticalDistanceB
                );
            }

            int horizontalDistanceA =
                GetHorizontalDistanceSquared(
                    coordinateA,
                    center
                );

            int horizontalDistanceB =
                GetHorizontalDistanceSquared(
                    coordinateB,
                    center
                );

            if (horizontalDistanceA !=
                horizontalDistanceB)
            {
                return horizontalDistanceA.CompareTo(
                    horizontalDistanceB
                );
            }

            if (coordinateA.Z !=
                coordinateB.Z)
            {
                return coordinateA.Z.CompareTo(
                    coordinateB.Z
                );
            }

            return coordinateA.X.CompareTo(
                coordinateB.X
            );
        }

        private static int GetHorizontalDistanceSquared(
            ChunkCoordinate coordinate,
            ChunkCoordinate center)
        {
            int dx =
                coordinate.X -
                center.X;

            int dz =
                coordinate.Z -
                center.Z;

            return
                dx * dx +
                dz * dz;
        }

        private void RemovePendingRequest(
            Chunk chunk)
        {
            if (pendingRequests.Count == 0)
                return;

            for (int i =
                    pendingRequests.Count - 1;
                i >= 0;
                i--)
            {
                GenerationRequest request =
                    pendingRequests[i];

                if (!ReferenceEquals(
                        request.Chunk,
                        chunk))
                {
                    continue;
                }

                pendingRequests.RemoveAt(i);

                pendingChunks.Remove(
                    chunk
                );

                return;
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
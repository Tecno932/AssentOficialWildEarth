using System;
using System.Collections.Generic;

namespace WildEarth.Voxel
{
    public sealed class FluidSimulationCoordinator : IDisposable
    {
        private readonly ChunkStorage chunkStorage;
        private readonly FluidScheduler scheduler;
        private readonly FluidSimulationCoordinatorSettings settings;

        private readonly Dictionary<
            ChunkCoordinate,
            FluidSimulationRunner
        > runners;

        private readonly Queue<FluidSimulationRequest>
            pendingSimulationRequests;

        private readonly HashSet<FluidSimulationRequest>
            pendingSimulationKeys;

        private bool disposed;

        public int RunningCount =>
            runners.Count;

        public int PendingSimulationCount =>
            pendingSimulationRequests.Count;

        public int MaxConcurrentSimulations =>
            settings.MaxConcurrentSimulations;

        public bool HasRunningSimulations =>
            runners.Count > 0;

        public bool HasPendingSimulations =>
            pendingSimulationRequests.Count > 0;

        public bool CanSchedule =>
            runners.Count <
            settings.MaxConcurrentSimulations;

        public FluidSimulationCoordinator(
            ChunkStorage chunkStorage,
            FluidScheduler scheduler,
            FluidSimulationCoordinatorSettings settings)
        {
            this.chunkStorage =
                chunkStorage ??
                throw new ArgumentNullException(
                    nameof(chunkStorage)
                );

            this.scheduler =
                scheduler ??
                throw new ArgumentNullException(
                    nameof(scheduler)
                );

            if (settings.MaxConcurrentSimulations < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settings),
                    "MaxConcurrentSimulations debe ser mayor que cero."
                );
            }

            this.settings = settings;

            runners =
                new Dictionary<
                    ChunkCoordinate,
                    FluidSimulationRunner
                >();

            pendingSimulationRequests =
                new Queue<FluidSimulationRequest>();

            pendingSimulationKeys =
                new HashSet<FluidSimulationRequest>();

            disposed = false;
        }

        public bool IsRunning(
            ChunkCoordinate coordinate)
        {
            ThrowIfDisposed();

            return runners.ContainsKey(
                coordinate
            );
        }

        public bool RequestSimulation(
            FluidSimulationRequest request)
        {
            ThrowIfDisposed();

            if (pendingSimulationKeys.Contains(request))
                return false;

            pendingSimulationKeys.Add(request);

            pendingSimulationRequests.Enqueue(
                request
            );

            return true;
        }

        public bool RequestChunkSimulation(
            ChunkCoordinate coordinate,
            FluidRuntimeDatabase fluidDatabase)
        {
            ThrowIfDisposed();

            if (fluidDatabase == null)
                throw new ArgumentNullException(
                    nameof(fluidDatabase)
                );

            if (!fluidDatabase.IsCreated)
                throw new InvalidOperationException(
                    "FluidRuntimeDatabase no está inicializada."
                );

            if (!chunkStorage.TryGet(
                    coordinate,
                    out Chunk chunk))
            {
                return false;
            }

            if (chunk == null)
                return false;

            if (
                chunk.State != ChunkState.Generated &&
                chunk.State != ChunkState.Ready)
            {
                return false;
            }

            if (!FluidChunkUtility.ContainsFluids(
                    chunk.Data,
                    fluidDatabase))
            {
                return false;
            }

            return RequestSimulation(
                new FluidSimulationRequest(
                    coordinate
                )
            );
        }

        public bool TryScheduleNext(
            FluidRuntimeDatabase fluidDatabase,
            FluidSimulationSettings simulationSettings)
        {
            ThrowIfDisposed();

            if (!CanSchedule)
                return false;

            if (pendingSimulationRequests.Count == 0)
                return false;

            FluidSimulationRequest request =
                pendingSimulationRequests.Peek();

            ChunkCoordinate coordinate =
                request.Chunk;

            if (runners.ContainsKey(coordinate))
            {
                RemovePendingRequest();

                return false;
            }

            if (!chunkStorage.TryGet(
                    coordinate,
                    out Chunk chunk))
            {
                RemovePendingRequest();

                return false;
            }

            if (chunk == null)
            {
                RemovePendingRequest();

                return false;
            }

            bool scheduled =
                TrySchedule(
                    coordinate,
                    fluidDatabase,
                    simulationSettings
                );

            if (!scheduled)
                return false;

            RemovePendingRequest();

            return true;
        }

        public int SchedulePending(
            FluidRuntimeDatabase fluidDatabase,
            FluidSimulationSettings simulationSettings)
        {
            ThrowIfDisposed();

            int scheduled = 0;

            while (
                CanSchedule &&
                pendingSimulationRequests.Count > 0)
            {
                int pendingBefore =
                    pendingSimulationRequests.Count;

                bool scheduledNext =
                    TryScheduleNext(
                        fluidDatabase,
                        simulationSettings
                    );

                if (scheduledNext)
                {
                    scheduled++;
                    continue;
                }

                /*
                * TryScheduleNext() puede consumir una solicitud
                * inválida, por ejemplo:
                *
                * - chunk inexistente
                * - chunk que ya está ejecutándose
                *
                * En ese caso seguimos buscando otra solicitud válida.
                */
                if (
                    pendingSimulationRequests.Count <
                    pendingBefore)
                {
                    continue;
                }

                /*
                * La solicitud sigue en la cola pero no pudo
                * programarse. No seguimos intentando para evitar
                * un bucle infinito.
                */
                break;
            }

            return scheduled;
        }

        public int CompleteFinishedAndSchedulePending(
            FluidRuntimeDatabase fluidDatabase,
            FluidSimulationSettings simulationSettings)
        {
            ThrowIfDisposed();

            int completed =
                CompleteFinished();

            int scheduled =
                SchedulePending(
                    fluidDatabase,
                    simulationSettings
                );

            return completed + scheduled;
        }

        public bool TrySchedule(
            ChunkCoordinate coordinate,
            FluidRuntimeDatabase fluidDatabase,
            FluidSimulationSettings simulationSettings)
        {
            ThrowIfDisposed();

            if (runners.ContainsKey(coordinate))
                return false;

            if (!CanSchedule)
                return false;

            if (!chunkStorage.TryGet(
                    coordinate,
                    out Chunk chunk))
            {
                return false;
            }

            if (chunk == null)
                return false;

            FluidSimulationRunner runner =
                new FluidSimulationRunner(
                    fluidDatabase,
                    simulationSettings
                );

            try
            {
                runner.Schedule(
                    chunk.Data,
                    coordinate
                );
            }
            catch
            {
                runner.Dispose();
                throw;
            }

            runners.Add(
                coordinate,
                runner
            );

            return true;
        }

        public bool TryComplete(
            ChunkCoordinate coordinate)
        {
            ThrowIfDisposed();

            if (!runners.TryGetValue(
                    coordinate,
                    out FluidSimulationRunner runner))
            {
                return false;
            }

            runner.Complete();

            return true;
        }

        public int CompleteAndEnqueue(
            ChunkCoordinate coordinate)
        {
            ThrowIfDisposed();

            if (!runners.TryGetValue(
                    coordinate,
                    out FluidSimulationRunner runner))
            {
                return 0;
            }

            try
            {
                int added =
                    runner.EnqueueResults(
                        scheduler
                    );

                RemoveRunner(
                    coordinate
                );

                return added;
            }
            catch
            {
                RemoveRunner(
                    coordinate
                );

                throw;
            }
        }

        public int CompleteFinished()
        {
            ThrowIfDisposed();

            if (runners.Count == 0)
                return 0;

            List<ChunkCoordinate> completedCoordinates =
                new List<ChunkCoordinate>();

            foreach (
                KeyValuePair<
                    ChunkCoordinate,
                    FluidSimulationRunner
                > pair
                in runners)
            {
                if (pair.Value.IsCompleted)
                {
                    completedCoordinates.Add(
                        pair.Key
                    );
                }
            }

            int added = 0;

            foreach (
                ChunkCoordinate coordinate
                in completedCoordinates)
            {
                added +=
                    CompleteAndEnqueue(
                        coordinate
                    );
            }

            return added;
        }

        public int CompleteAll()
        {
            ThrowIfDisposed();

            if (runners.Count == 0)
                return 0;

            List<ChunkCoordinate> coordinates =
                new List<ChunkCoordinate>(
                    runners.Keys
                );

            int added = 0;

            foreach (
                ChunkCoordinate coordinate
                in coordinates)
            {
                added +=
                    CompleteAndEnqueue(
                        coordinate
                    );
            }

            return added;
        }

        public bool Remove(
            ChunkCoordinate coordinate)
        {
            ThrowIfDisposed();

            if (!runners.TryGetValue(
                    coordinate,
                    out FluidSimulationRunner runner))
            {
                return false;
            }

            runner.Dispose();

            runners.Remove(
                coordinate
            );

            return true;
        }

        public void Clear()
        {
            ThrowIfDisposed();

            foreach (
                FluidSimulationRunner runner
                in runners.Values)
            {
                runner.Dispose();
            }

            runners.Clear();

            pendingSimulationRequests.Clear();
            pendingSimulationKeys.Clear();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            foreach (
                FluidSimulationRunner runner
                in runners.Values)
            {
                runner.Dispose();
            }

            runners.Clear();

            pendingSimulationRequests.Clear();
            pendingSimulationKeys.Clear();

            disposed = true;
        }

        private void RemovePendingRequest()
        {
            FluidSimulationRequest request =
                pendingSimulationRequests.Dequeue();

            pendingSimulationKeys.Remove(
                request
            );
        }

        private void RemoveRunner(
            ChunkCoordinate coordinate)
        {
            if (!runners.TryGetValue(
                    coordinate,
                    out FluidSimulationRunner runner))
            {
                return;
            }

            runner.Dispose();

            runners.Remove(
                coordinate
            );
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(
                    nameof(FluidSimulationCoordinator)
                );
            }
        }
    }
}
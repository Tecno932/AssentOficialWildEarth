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

        private bool disposed;

        public int RunningCount =>
            runners.Count;

        public int MaxConcurrentSimulations =>
            settings.MaxConcurrentSimulations;

        public bool HasRunningSimulations =>
            runners.Count > 0;

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

            disposed = true;
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
using System;
using Unity.Collections;
using Unity.Jobs;

namespace WildEarth.Voxel
{
    public sealed class FluidSimulationRunner : IDisposable
    {
        private readonly FluidRuntimeDatabase fluidDatabase;
        private readonly FluidSimulationSettings settings;

        private FluidSimulationResult result;
        private JobHandle jobHandle;

        private bool isScheduled;
        private bool disposed;

        public bool IsScheduled =>
            isScheduled;

        public bool IsCompleted =>
            !isScheduled ||
            jobHandle.IsCompleted;

        public FluidSimulationResult Result
        {
            get
            {
                ThrowIfDisposed();

                if (!isScheduled)
                {
                    throw new InvalidOperationException(
                        "No hay una simulación de fluidos programada."
                    );
                }

                return result;
            }
        }

        public FluidSimulationRunner(
            FluidRuntimeDatabase fluidDatabase,
            FluidSimulationSettings settings)
        {
            this.fluidDatabase =
                fluidDatabase ??
                throw new ArgumentNullException(
                    nameof(fluidDatabase)
                );

            this.settings = settings;

            result = null;
            jobHandle = default;
            isScheduled = false;
            disposed = false;
        }

        public void Schedule(
            ChunkData chunkData,
            ChunkCoordinate chunkCoordinate)
        {
            ThrowIfDisposed();

            if (chunkData == null)
            {
                throw new ArgumentNullException(
                    nameof(chunkData)
                );
            }

            if (!chunkData.IsCreated)
            {
                throw new InvalidOperationException(
                    "El ChunkData no contiene un NativeArray creado."
                );
            }

            if (isScheduled)
            {
                throw new InvalidOperationException(
                    "Ya existe una simulación de fluidos programada."
                );
            }

            if (!fluidDatabase.IsCreated)
            {
                throw new InvalidOperationException(
                    "FluidRuntimeDatabase no está inicializada."
                );
            }

            result =
                new FluidSimulationResult(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Persistent
                );

            FluidSimulationJob job =
                new FluidSimulationJob
                {
                    Voxels = chunkData.Voxels,
                    FluidsByBlockId =
                        fluidDatabase.AsBlockLookupNativeArray(),
                    Settings = settings,
                    ChunkCoordinate = chunkCoordinate,
                    Changes = result.Changes,
                    ChangeCounts = result.ChangeCounts
                };

            jobHandle =
                job.Schedule(
                    VoxelConstants.VoxelsPerChunk,
                    64,
                    default
                );

            isScheduled = true;
        }

        public void Complete()
        {
            ThrowIfDisposed();

            if (!isScheduled)
                return;

            jobHandle.Complete();
        }

        public int EnqueueResults(
            FluidScheduler scheduler)
        {
            ThrowIfDisposed();

            if (scheduler == null)
            {
                throw new ArgumentNullException(
                    nameof(scheduler)
                );
            }

            if (!isScheduled)
            {
                throw new InvalidOperationException(
                    "No hay una simulación programada."
                );
            }

            Complete();

            int added = 0;

            for (
                int voxelIndex = 0;
                voxelIndex < result.VoxelCount;
                voxelIndex++)
            {
                int changeCount =
                    result.GetChangeCount(
                        voxelIndex
                    );

                for (
                    int changeIndex = 0;
                    changeIndex < changeCount;
                    changeIndex++)
                {
                    FluidChange change =
                        result.GetChange(
                            voxelIndex,
                            changeIndex
                        );

                    FluidPendingUpdate update =
                        new FluidPendingUpdate(
                            change.TargetChunk,
                            change.X,
                            change.Y,
                            change.Z,
                            change.State
                        );

                    if (scheduler.Enqueue(update))
                        added++;
                }
            }

            return added;
        }

        public void CompleteAndEnqueue(
            FluidScheduler scheduler)
        {
            EnqueueResults(scheduler);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (isScheduled)
            {
                jobHandle.Complete();
                isScheduled = false;
            }

            if (result != null)
            {
                result.Dispose();
                result = null;
            }

            jobHandle = default;
            disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(
                    nameof(FluidSimulationRunner)
                );
            }
        }
    }
}
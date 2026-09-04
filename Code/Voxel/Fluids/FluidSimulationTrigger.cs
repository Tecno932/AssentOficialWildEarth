using System;

namespace WildEarth.Voxel
{
    public sealed class FluidSimulationTrigger
    {
        private readonly FluidSimulationCoordinator coordinator;
        private readonly FluidRuntimeDatabase fluidDatabase;

        public FluidSimulationTrigger(
            FluidSimulationCoordinator coordinator,
            FluidRuntimeDatabase fluidDatabase)
        {
            this.coordinator =
                coordinator ??
                throw new ArgumentNullException(
                    nameof(coordinator)
                );

            this.fluidDatabase =
                fluidDatabase ??
                throw new ArgumentNullException(
                    nameof(fluidDatabase)
                );

            if (!fluidDatabase.IsCreated)
            {
                throw new InvalidOperationException(
                    "FluidRuntimeDatabase no está inicializada."
                );
            }
        }

        public bool Trigger(
            ChunkCoordinate coordinate)
        {
            return coordinator.RequestChunkSimulation(
                coordinate,
                fluidDatabase
            );
        }
    }
}
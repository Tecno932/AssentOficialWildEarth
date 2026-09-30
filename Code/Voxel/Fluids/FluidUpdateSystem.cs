using System;

namespace WildEarth.Voxel
{
    public sealed class FluidUpdateSystem
    {
        private readonly ChunkStorage chunkStorage;
        private readonly FluidRuntimeDatabase fluidDatabase;
        private readonly FluidSimulationSettings settings;
        public FluidRuntimeDatabase FluidDatabase =>
            fluidDatabase;

        public FluidUpdateSystem(
            ChunkStorage chunkStorage,
            FluidRuntimeDatabase fluidDatabase,
            FluidSimulationSettings settings)
        {
            this.chunkStorage =
                chunkStorage ??
                throw new ArgumentNullException(
                    nameof(chunkStorage)
                );

            this.fluidDatabase =
                fluidDatabase ??
                throw new ArgumentNullException(
                    nameof(fluidDatabase)
                );

            this.settings = settings;
        }

        public bool TryApply(
            FluidChange change,
            out FluidChangeResult result)
        {
            result =
                default;

            if (!chunkStorage.TryGet(
                    change.TargetChunk,
                    out Chunk targetChunk))
            {
                result =
                    new FluidChangeResult(
                        change,
                        false,
                        false,
                        false,
                        false
                    );

                return false;
            }

            if (targetChunk == null ||
                !targetChunk.Data.IsCreated ||
                (targetChunk.State != ChunkState.Generated &&
                targetChunk.State != ChunkState.Ready))
            {
                result =
                    new FluidChangeResult(
                        change,
                        false,
                        false,
                        false,
                        false
                    );

                return false;
            }

            if (!change.IsValid)
            {
                result =
                    new FluidChangeResult(
                        change,
                        false,
                        true,
                        false,
                        false
                    );

                return false;
            }

            Voxel current =
                ChunkDataAccess.GetVoxel(
                    targetChunk.Data,
                    change.X,
                    change.Y,
                    change.Z
                );

            bool targetWasAir =
                current.IsAir;

            UnityEngine.Debug.Log(
                $"[FluidTargetDebug] " +
                $"Chunk={change.TargetChunk} " +
                $"XYZ=({change.X},{change.Y},{change.Z}) " +
                $"BlockId={current.BlockId} " +
                $"State={current.State} " +
                $"IsAir={current.IsAir} " +
                $"IsFluid={fluidDatabase.TryGetByBlockId(current.BlockId, out FluidRuntimeData targetFluid)} " +
                $"TargetFluidType={(targetFluid.IsValid ? targetFluid.Type.ToString() : "None")} " +
                $"TargetFluidLevel={(targetFluid.IsValid ? current.State.ToString() : "0")}"
            );

            /*
            * Empty significa eliminar el fluido existente.
            *
            * Nunca elimina bloques sólidos ni otros tipos
            * de fluidos.
            */
            if (change.State.IsEmpty)
            {
                if (!TryGetFluidState(
                        current,
                        out FluidState fluidToRemove))
                {
                    result =
                        new FluidChangeResult(
                            change,
                            false,
                            true,
                            false,
                            false
                        );

                    return false;
                }

                Voxel air =
                    new Voxel(
                        BlockIds.Air,
                        current.Light,
                        0
                    );

                ChunkDataAccess.SetVoxel(
                    targetChunk.Data,
                    change.X,
                    change.Y,
                    change.Z,
                    air
                );

                MarkChunkDirty(
                    targetChunk
                );

                result =
                    new FluidChangeResult(
                        change,
                        true,
                        true,
                        false,
                        false
                    );

                return true;
            }

            if (!fluidDatabase.TryGet(
                    change.State.Type,
                    out FluidRuntimeData fluid))
            {
                result =
                    new FluidChangeResult(
                        change,
                        false,
                        true,
                        false,
                        false
                    );

                return false;
            }

            if (targetWasAir)
            {
                ApplyFluid(
                    targetChunk,
                    change,
                    fluid
                );

                MarkChunkDirty(
                    targetChunk
                );

                result =
                    new FluidChangeResult(
                        change,
                        true,
                        true,
                        true,
                        false
                    );

                return true;
            }

            if (TryGetFluidState(
                    current,
                    out FluidState currentFluid))
            {
                if (currentFluid.Type !=
                    change.State.Type)
                {
                    result =
                        new FluidChangeResult(
                            change,
                            false,
                            true,
                            false,
                            false
                        );

                    return false;
                }

                if (!FluidPropagation.ShouldReplace(
                        currentFluid,
                        change.State))
                {
                    result =
                        new FluidChangeResult(
                            change,
                            false,
                            true,
                            false,
                            false
                        );

                    return false;
                }

                ApplyFluid(
                    targetChunk,
                    change,
                    fluid
                );

                MarkChunkDirty(
                    targetChunk
                );

                result =
                    new FluidChangeResult(
                        change,
                        true,
                        true,
                        false,
                        true
                    );

                return true;
            }

            result =
                new FluidChangeResult(
                    change,
                    false,
                    true,
                    false,
                    false
                );

            return false;
        }

        private bool TryGetFluidState(
            Voxel voxel,
            out FluidState state)
        {
            if (!fluidDatabase.TryGetByBlockId(
                    voxel.BlockId,
                    out FluidRuntimeData fluid))
            {
                state = default;
                return false;
            }

            state =
                new FluidState(
                    fluid.Type,
                    voxel.State
                );

            return !state.IsEmpty;
        }

        private static void ApplyFluid(
            Chunk chunk,
            FluidChange change,
            FluidRuntimeData fluid)
        {
            Voxel voxel =
                new Voxel(
                    fluid.BlockId,
                    0,
                    change.State.Level
                );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                change.X,
                change.Y,
                change.Z,
                voxel
            );
        }

        private static void MarkChunkDirty(
            Chunk chunk)
        {
            chunk.MarkVoxelDataChanged();
        }
    }
}
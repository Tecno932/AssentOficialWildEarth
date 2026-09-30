using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace WildEarth.Voxel
{
    [BurstCompile]
    public struct FluidSimulationJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Voxel> Voxels;

        [ReadOnly]
        public NativeArray<FluidRuntimeData> FluidsByBlockId;

        public FluidSimulationSettings Settings;

        public ChunkCoordinate ChunkCoordinate;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<FluidChange> Changes;

        [WriteOnly]
        public NativeArray<byte> ChangeCounts;

        public void Execute(int index)
        {
            int changeCount = 0;

            Voxel voxel =
                Voxels[index];

            if (!TryGetFluid(
                    voxel.BlockId,
                    out FluidRuntimeData fluid))
            {
                ChangeCounts[index] = 0;
                return;
            }

            FluidState state =
                new FluidState(
                    fluid.Type,
                    voxel.State
                );

            if (state.IsEmpty)
            {
                ChangeCounts[index] = 0;
                return;
            }

            VoxelIndex.FromIndex(
                index,
                out int x,
                out int y,
                out int z
            );

            TryCreateVerticalChange(
                index,
                ref changeCount,
                x,
                y,
                z,
                state,
                fluid
            );

            if (Settings.AllowHorizontalFlow)
            {
                byte horizontalLevel =
                    FluidPropagation.CalculateHorizontalLevel(
                        state.Level,
                        fluid.HorizontalFlowDecay
                    );

                if (horizontalLevel != 0)
                {
                    TryCreateHorizontalChange(
                        index,
                        ref changeCount,
                        x + 1,
                        y,
                        z,
                        state,
                        horizontalLevel
                    );

                    TryCreateHorizontalChange(
                        index,
                        ref changeCount,
                        x - 1,
                        y,
                        z,
                        state,
                        horizontalLevel
                    );

                    TryCreateHorizontalChange(
                        index,
                        ref changeCount,
                        x,
                        y,
                        z + 1,
                        state,
                        horizontalLevel
                    );

                    TryCreateHorizontalChange(
                        index,
                        ref changeCount,
                        x,
                        y,
                        z - 1,
                        state,
                        horizontalLevel
                    );
                }
            }

            ChangeCounts[index] =
                (byte)changeCount;
        }

        private bool TryGetFluid(
            ushort blockId,
            out FluidRuntimeData fluid)
        {
            if (blockId == BlockIds.Air ||
                blockId >= FluidsByBlockId.Length)
            {
                fluid = default;
                return false;
            }

            fluid =
                FluidsByBlockId[blockId];

            return fluid.IsValid;
        }

        private void TryCreateVerticalChange(
            int sourceIndex,
            ref int changeCount,
            int x,
            int y,
            int z,
            FluidState state,
            FluidRuntimeData fluid)
        {
            if (!Settings.AllowVerticalFlow)
                return;

            int targetX = x;
            int targetY = y - 1;
            int targetZ = z;

            byte level =
                FluidPropagation.CalculateVerticalLevel(
                    state.Level,
                    fluid.VerticalFlowDecay
                );

            if (level == 0)
                return;

            ChunkCoordinate targetChunk =
                ChunkCoordinateUtility.ResolveChunk(
                    ChunkCoordinate,
                    targetX,
                    targetY,
                    targetZ,
                    out targetX,
                    out targetY,
                    out targetZ
                );

            if (targetChunk == ChunkCoordinate)
            {
                int targetIndex =
                    VoxelIndex.ToIndex(
                        targetX,
                        targetY,
                        targetZ
                    );

                Voxel target =
                    Voxels[targetIndex];

                if (!target.IsAir)
                {
                    if (!TryGetFluid(
                            target.BlockId,
                            out FluidRuntimeData targetFluid))
                    {
                        return;
                    }

                    if (targetFluid.Type != state.Type)
                    {
                        return;
                    }

                    if (target.State >= level)
                    {
                        return;
                    }
                }
            }

            WriteChange(
                sourceIndex,
                ref changeCount,
                new FluidChange(
                    targetChunk,
                    targetX,
                    targetY,
                    targetZ,
                    new FluidState(
                        state.Type,
                        level
                    )
                )
            );
        }

        private void TryCreateHorizontalChange(
            int sourceIndex,
            ref int changeCount,
            int x,
            int y,
            int z,
            FluidState state,
            byte level)
        {
            if (y < 0 ||
                y >= VoxelConstants.ChunkSize)
            {
                return;
            }

            ChunkCoordinate targetChunk =
                ChunkCoordinateUtility.ResolveChunk(
                    ChunkCoordinate,
                    x,
                    y,
                    z,
                    out int targetX,
                    out int targetY,
                    out int targetZ
                );

            /*
             * Si el destino permanece dentro del chunk,
             * podemos comprobarlo directamente.
             *
             * Si cruza el límite del chunk, se genera el
             * cambio pendiente para que FluidUpdateSystem
             * lo valide posteriormente.
             */
            if (targetChunk == ChunkCoordinate)
            {
                int targetIndex =
                    VoxelIndex.ToIndex(
                        targetX,
                        targetY,
                        targetZ
                    );

                Voxel target =
                    Voxels[targetIndex];

                if (!target.IsAir)
                {
                    if (!TryGetFluid(
                            target.BlockId,
                            out FluidRuntimeData targetFluid))
                    {
                        return;
                    }

                    if (targetFluid.Type != state.Type)
                    {
                        return;
                    }

                    if (target.State >= level)
                    {
                        return;
                    }
                }
            }

            WriteChange(
                sourceIndex,
                ref changeCount,
                new FluidChange(
                    targetChunk,
                    targetX,
                    targetY,
                    targetZ,
                    new FluidState(
                        state.Type,
                        level
                    )
                )
            );
        }

        private void WriteChange(
            int sourceIndex,
            ref int changeCount,
            FluidChange change)
        {
            if (changeCount >=
                FluidChangeBuffer.MaxChangesPerVoxel)
            {
                return;
            }

            int outputIndex =
                sourceIndex *
                FluidChangeBuffer.MaxChangesPerVoxel +
                changeCount;

            Changes[outputIndex] =
                change;

            changeCount++;
        }
    }
}
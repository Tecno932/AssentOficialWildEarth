using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

            TryCreateHorizontalChange(
                index,
                ref changeCount,
                x + 1,
                y,
                z,
                state,
                fluid
            );

            TryCreateHorizontalChange(
                index,
                ref changeCount,
                x - 1,
                y,
                z,
                state,
                fluid
            );

            TryCreateHorizontalChange(
                index,
                ref changeCount,
                x,
                y,
                z + 1,
                state,
                fluid
            );

            TryCreateHorizontalChange(
                index,
                ref changeCount,
                x,
                y,
                z - 1,
                state,
                fluid
            );

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

            if (y <= 0)
                return;

            int targetX;
            int targetY;
            int targetZ;

            ChunkCoordinate targetChunk =
                ChunkCoordinateUtility.ResolveChunk(
                    ChunkCoordinate,
                    x,
                    y - 1,
                    z,
                    out targetX,
                    out targetY,
                    out targetZ
                );

            /*
             * Si el destino sigue dentro del chunk actual,
             * podemos comprobar inmediatamente si está ocupado.
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
                    return;
            }

            byte level =
                FluidPropagation.CalculateVerticalLevel(
                    state.Level,
                    fluid.VerticalFlowDecay
                );

            if (level == 0)
                return;

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
            FluidRuntimeData fluid)
        {
            if (!Settings.AllowHorizontalFlow)
                return;

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

            byte level =
                FluidPropagation.CalculateHorizontalLevel(
                    state.Level,
                    fluid.HorizontalFlowDecay
                );

            if (level == 0)
                return;

            /*
             * No podemos consultar directamente el voxel
             * del chunk vecino desde este job porque este
             * job solo posee el NativeArray del chunk actual.
             *
             * Por lo tanto:
             *
             * - coordenada dentro del chunk actual:
             *   se puede comprobar contra Voxels.
             *
             * - coordenada fuera del chunk:
             *   se genera como cambio pendiente.
             *
             * La validación del voxel vecino se hará en
             * la fase de aplicación/scheduler.
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
                    return;
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
using System;

namespace WildEarth.Voxel
{
    public static class ChunkCoordinateUtility
    {
        public static ChunkCoordinate ResolveChunk(
            ChunkCoordinate chunk,
            int localX,
            int localY,
            int localZ,
            out int resolvedX,
            out int resolvedY,
            out int resolvedZ)
        {
            int chunkX = chunk.X;
            int chunkY = chunk.Y;
            int chunkZ = chunk.Z;

            ResolveAxis(
                localX,
                ref chunkX,
                out resolvedX
            );

            ResolveAxis(
                localY,
                ref chunkY,
                out resolvedY
            );

            ResolveAxis(
                localZ,
                ref chunkZ,
                out resolvedZ
            );

            return new ChunkCoordinate(
                chunkX,
                chunkY,
                chunkZ
            );
        }

        public static ChunkCoordinate ResolveWorld(
            int worldX,
            int worldY,
            int worldZ,
            out int localX,
            out int localY,
            out int localZ)
        {
            int chunkX =
                FloorDiv(
                    worldX,
                    VoxelConstants.ChunkSize
                );

            int chunkY =
                FloorDiv(
                    worldY,
                    VoxelConstants.ChunkSize
                );

            int chunkZ =
                FloorDiv(
                    worldZ,
                    VoxelConstants.ChunkSize
                );

            localX =
                FloorMod(
                    worldX,
                    VoxelConstants.ChunkSize
                );

            localY =
                FloorMod(
                    worldY,
                    VoxelConstants.ChunkSize
                );

            localZ =
                FloorMod(
                    worldZ,
                    VoxelConstants.ChunkSize
                );

            return new ChunkCoordinate(
                chunkX,
                chunkY,
                chunkZ
            );
        }

        private static void ResolveAxis(
            int local,
            ref int chunk,
            out int resolved)
        {
            int size =
                VoxelConstants.ChunkSize;

            int offset =
                FloorDiv(
                    local,
                    size
                );

            resolved =
                local -
                offset * size;

            chunk += offset;
        }

        private static int FloorDiv(
            int value,
            int divisor)
        {
            if (divisor <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(divisor),
                    "El divisor debe ser mayor que cero."
                );
            }

            int quotient =
                value / divisor;

            int remainder =
                value % divisor;

            if (remainder < 0)
                quotient--;

            return quotient;
        }

        private static int FloorMod(
            int value,
            int divisor)
        {
            if (divisor <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(divisor),
                    "El divisor debe ser mayor que cero."
                );
            }

            int result =
                value % divisor;

            if (result < 0)
                result += divisor;

            return result;
        }
    }
}
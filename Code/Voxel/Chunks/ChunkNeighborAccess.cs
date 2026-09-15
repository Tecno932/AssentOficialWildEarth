using System;

namespace WildEarth.Voxel
{
    public static class ChunkNeighborAccess
    {
        public static bool TryGetVoxel(
            ChunkStorage storage,
            ChunkCoordinate coordinate,
            int x,
            int y,
            int z,
            out Voxel voxel)
        {
            if (storage == null)
                throw new ArgumentNullException(
                    nameof(storage)
                );

            /*
             * Coordenada dentro del chunk actual.
             */
            if (VoxelIndex.IsValidLocalCoordinate(
                    x,
                    y,
                    z))
            {
                if (!storage.TryGet(
                        coordinate,
                        out Chunk chunk))
                {
                    voxel =
                        new Voxel(
                            BlockIds.Air
                        );

                    return false;
                }

                /*
                 * El chunk existe, pero sus datos todavía
                 * pueden estar siendo escritos por Jobs.
                 *
                 * Nunca leemos mientras está generándose.
                 */
                if (!IsVoxelDataReady(chunk))
                {
                    voxel =
                        new Voxel(
                            BlockIds.Air
                        );

                    return false;
                }

                voxel =
                    ChunkDataAccess.GetVoxel(
                        chunk.Data,
                        x,
                        y,
                        z
                    );

                return true;
            }

            /*
             * Coordenada fuera del chunk actual.
             */
            ChunkNeighborDirection direction;

            int neighborX = x;
            int neighborY = y;
            int neighborZ = z;

            if (x < 0)
            {
                direction =
                    ChunkNeighborDirection.West;

                neighborX =
                    VoxelConstants.ChunkSize - 1;
            }
            else if (x >= VoxelConstants.ChunkSize)
            {
                direction =
                    ChunkNeighborDirection.East;

                neighborX = 0;
            }
            else if (y < 0)
            {
                direction =
                    ChunkNeighborDirection.Below;

                neighborY =
                    VoxelConstants.ChunkSize - 1;
            }
            else if (y >= VoxelConstants.ChunkSize)
            {
                direction =
                    ChunkNeighborDirection.Above;

                neighborY = 0;
            }
            else if (z < 0)
            {
                direction =
                    ChunkNeighborDirection.South;

                neighborZ =
                    VoxelConstants.ChunkSize - 1;
            }
            else
            {
                direction =
                    ChunkNeighborDirection.North;

                neighborZ = 0;
            }

            if (!ChunkNeighborResolver.TryGetNeighbor(
                    storage,
                    coordinate,
                    direction,
                    out Chunk neighbor))
            {
                voxel =
                    new Voxel(
                        BlockIds.Air
                    );

                return false;
            }

            /*
             * El vecino existe, pero puede seguir generándose.
             * En ese caso NO tocamos su NativeArray.
             */
            if (!IsVoxelDataReady(neighbor))
            {
                voxel =
                    new Voxel(
                        BlockIds.Air
                    );

                return false;
            }

            voxel =
                ChunkDataAccess.GetVoxel(
                    neighbor.Data,
                    neighborX,
                    neighborY,
                    neighborZ
                );

            return true;
        }

        public static ushort GetBlockId(
            ChunkStorage storage,
            ChunkCoordinate coordinate,
            int x,
            int y,
            int z,
            out bool resolved)
        {
            if (storage == null)
                throw new ArgumentNullException(
                    nameof(storage)
                );

            resolved =
                TryGetVoxel(
                    storage,
                    coordinate,
                    x,
                    y,
                    z,
                    out Voxel voxel
                );

            return voxel.BlockId;
        }

        public static bool IsBoundary(
            int x,
            int y,
            int z,
            ChunkNeighborDirection direction)
        {
            switch (direction)
            {
                case ChunkNeighborDirection.North:
                    return z ==
                           VoxelConstants.ChunkSize - 1;

                case ChunkNeighborDirection.South:
                    return z == 0;

                case ChunkNeighborDirection.East:
                    return x ==
                           VoxelConstants.ChunkSize - 1;

                case ChunkNeighborDirection.West:
                    return x == 0;

                case ChunkNeighborDirection.Above:
                    return y ==
                           VoxelConstants.ChunkSize - 1;

                case ChunkNeighborDirection.Below:
                    return y == 0;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Dirección de vecino inválida."
                    );
            }
        }

        private static bool IsVoxelDataReady(
            Chunk chunk)
        {
            if (chunk == null)
                return false;

            return chunk.State ==
                       ChunkState.Generated ||
                   chunk.State ==
                       ChunkState.Ready;
        }
    }
}
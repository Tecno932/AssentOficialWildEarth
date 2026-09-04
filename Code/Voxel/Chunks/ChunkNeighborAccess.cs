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
                throw new ArgumentNullException(nameof(storage));

            /*
             * Primero intentamos acceder al voxel dentro
             * del chunk actual.
             */
            if (VoxelIndex.IsValidLocalCoordinate(
                    x,
                    y,
                    z))
            {
                if (storage.TryGet(
                        coordinate,
                        out Chunk chunk))
                {
                    voxel =
                        ChunkDataAccess.GetVoxel(
                            chunk.Data,
                            x,
                            y,
                            z
                        );

                    return true;
                }

                voxel =
                    new Voxel(BlockIds.Air);

                return false;
            }

            /*
             * La coordenada está fuera del chunk.
             * Determinamos qué vecino corresponde.
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
                /*
                 * El vecino todavía no está cargado.
                 *
                 * No confundimos "no cargado" con un bloque Air.
                 * El bool indica que no pudimos resolver el voxel.
                 */
                voxel =
                    new Voxel(BlockIds.Air);

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
                throw new ArgumentNullException(nameof(storage));

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
                    return z == VoxelConstants.ChunkSize - 1;

                case ChunkNeighborDirection.South:
                    return z == 0;

                case ChunkNeighborDirection.East:
                    return x == VoxelConstants.ChunkSize - 1;

                case ChunkNeighborDirection.West:
                    return x == 0;

                case ChunkNeighborDirection.Above:
                    return y == VoxelConstants.ChunkSize - 1;

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
    }
}
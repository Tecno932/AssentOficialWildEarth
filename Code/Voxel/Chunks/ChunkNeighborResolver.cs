using System;

namespace WildEarth.Voxel
{
    public static class ChunkNeighborResolver
    {
        public static ChunkCoordinate GetNeighborCoordinate(
            ChunkCoordinate coordinate,
            ChunkNeighborDirection direction)
        {
            switch (direction)
            {
                case ChunkNeighborDirection.North:
                    return new ChunkCoordinate(
                        coordinate.X,
                        coordinate.Y,
                        coordinate.Z + 1
                    );

                case ChunkNeighborDirection.South:
                    return new ChunkCoordinate(
                        coordinate.X,
                        coordinate.Y,
                        coordinate.Z - 1
                    );

                case ChunkNeighborDirection.East:
                    return new ChunkCoordinate(
                        coordinate.X + 1,
                        coordinate.Y,
                        coordinate.Z
                    );

                case ChunkNeighborDirection.West:
                    return new ChunkCoordinate(
                        coordinate.X - 1,
                        coordinate.Y,
                        coordinate.Z
                    );

                case ChunkNeighborDirection.Above:
                    return new ChunkCoordinate(
                        coordinate.X,
                        coordinate.Y + 1,
                        coordinate.Z
                    );

                case ChunkNeighborDirection.Below:
                    return new ChunkCoordinate(
                        coordinate.X,
                        coordinate.Y - 1,
                        coordinate.Z
                    );

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(direction),
                        direction,
                        "Dirección de vecino inválida."
                    );
            }
        }

        public static bool TryGetNeighbor(
            ChunkStorage storage,
            ChunkCoordinate coordinate,
            ChunkNeighborDirection direction,
            out Chunk neighbor)
        {
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));

            ChunkCoordinate neighborCoordinate =
                GetNeighborCoordinate(
                    coordinate,
                    direction
                );

            return storage.TryGet(
                neighborCoordinate,
                out neighbor
            );
        }

        public static Chunk GetNeighbor(
            ChunkStorage storage,
            ChunkCoordinate coordinate,
            ChunkNeighborDirection direction)
        {
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));

            ChunkCoordinate neighborCoordinate =
                GetNeighborCoordinate(
                    coordinate,
                    direction
                );

            storage.TryGet(
                neighborCoordinate,
                out Chunk neighbor
            );

            return neighbor;
        }
    }
}
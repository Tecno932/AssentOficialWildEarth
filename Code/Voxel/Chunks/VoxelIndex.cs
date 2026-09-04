namespace WildEarth.Voxel
{
    /// <summary>
    /// Conversión entre coordenadas 3D locales y el índice lineal
    /// utilizado por el almacenamiento del chunk.
    /// </summary>
    public static class VoxelIndex
    {
        /// <summary>
        /// Convierte coordenadas locales en un índice lineal.
        ///
        /// Layout:
        ///
        /// X → eje más rápido
        /// Z → segundo eje
        /// Y → eje más lento
        /// </summary>
        public static int ToIndex(
            int x,
            int y,
            int z)
        {
            return x +
                   z * VoxelConstants.ChunkSize +
                   y * VoxelConstants.ChunkSize *
                       VoxelConstants.ChunkSize;
        }

        public static int ToIndex(
            int x,
            int y,
            int z,
            int chunkSize)
        {
            return x +
                   z * chunkSize +
                   y * chunkSize * chunkSize;
        }

        public static void FromIndex(
            int index,
            out int x,
            out int y,
            out int z)
        {
            int area = VoxelConstants.ChunkSize *
                       VoxelConstants.ChunkSize;

            y = index / area;

            int remainder = index - y * area;

            z = remainder / VoxelConstants.ChunkSize;

            x = remainder -
                z * VoxelConstants.ChunkSize;
        }

        public static bool IsValidLocalCoordinate(
            int x,
            int y,
            int z)
        {
            return x >= 0 &&
                   x < VoxelConstants.ChunkSize &&
                   y >= 0 &&
                   y < VoxelConstants.ChunkSize &&
                   z >= 0 &&
                   z < VoxelConstants.ChunkSize;
        }
    }
}
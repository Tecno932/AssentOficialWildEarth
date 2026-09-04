namespace WildEarth.Voxel
{
    /// <summary>
    /// Constantes estructurales del sistema voxel.
    /// </summary>
    public static class VoxelConstants
    {
        /// <summary>
        /// Tamaño horizontal y vertical de un chunk.
        /// </summary>
        public const int ChunkSize = 16;

        /// <summary>
        /// Altura total del mundo.
        /// </summary>
        public const int WorldHeight = 256;

        /// <summary>
        /// Cantidad de voxels contenidos en un chunk.
        /// </summary>
        public const int VoxelsPerChunk =
            ChunkSize *
            ChunkSize *
            ChunkSize;

        /// <summary>
        /// Cantidad de chunks verticales necesarios
        /// para representar los 256 bloques de altura.
        /// </summary>
        public const int ChunksVertical =
            WorldHeight / ChunkSize;

        /// <summary>
        /// Altura máxima de un voxel válido.
        /// </summary>
        public const int MaxVoxelY = WorldHeight - 1;

        /// <summary>
        /// Altura mínima del mundo.
        ///
        /// Por ahora utilizamos 0 como origen vertical.
        /// Posteriormente podemos introducir niveles negativos
        /// mediante una configuración de dimensión.
        /// </summary>
        public const int MinVoxelY = 0;
    }
}
using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public readonly struct VoxelWorldSettings
    {
        public readonly int InitialChunkPoolSize;
        public readonly int MaximumChunkPoolSize;
        public readonly int InitialChunkStorageCapacity;

        public VoxelWorldSettings(
            int initialChunkPoolSize,
            int maximumChunkPoolSize,
            int initialChunkStorageCapacity)
        {
            if (initialChunkPoolSize < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(initialChunkPoolSize));

            if (maximumChunkPoolSize <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(maximumChunkPoolSize));

            if (initialChunkPoolSize > maximumChunkPoolSize)
                throw new ArgumentException(
                    "El pool inicial no puede superar el máximo."
                );

            if (initialChunkStorageCapacity <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(initialChunkStorageCapacity));

            InitialChunkPoolSize = initialChunkPoolSize;
            MaximumChunkPoolSize = maximumChunkPoolSize;
            InitialChunkStorageCapacity =
                initialChunkStorageCapacity;
        }

        public static VoxelWorldSettings Default =>
            new VoxelWorldSettings(
                initialChunkPoolSize: 64,
                maximumChunkPoolSize: 4096,
                initialChunkStorageCapacity: 1024
            );
    }
}
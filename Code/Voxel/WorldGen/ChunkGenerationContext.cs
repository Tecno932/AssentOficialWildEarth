using Unity.Mathematics;

namespace WildEarth.Voxel
{
    public readonly struct ChunkGenerationContext
    {
        public readonly int Seed;
        public readonly int3 ChunkCoordinate;
        public readonly int3 WorldOrigin;

        public ChunkGenerationContext(
            int seed,
            int3 chunkCoordinate)
        {
            Seed = seed;
            ChunkCoordinate = chunkCoordinate;

            WorldOrigin =
                chunkCoordinate *
                VoxelConstants.ChunkSize;
        }

        public int3 GetWorldPosition(
            int localX,
            int localY,
            int localZ)
        {
            return WorldOrigin +
                   new int3(
                       localX,
                       localY,
                       localZ
                   );
        }
    }
}
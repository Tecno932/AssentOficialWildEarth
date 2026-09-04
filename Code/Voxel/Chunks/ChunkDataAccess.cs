namespace WildEarth.Voxel
{
    public static class ChunkDataAccess
    {
        public static Voxel GetVoxel(
            ChunkData chunk,
            int x,
            int y,
            int z)
        {
            if (chunk == null)
            {
                return new Voxel(BlockIds.Air);
            }

            if (!VoxelIndex.IsValidLocalCoordinate(x, y, z))
            {
                return new Voxel(BlockIds.Air);
            }

            int index = VoxelIndex.ToIndex(x, y, z);

            return chunk.Voxels[index];
        }

        public static void SetVoxel(
            ChunkData chunk,
            int x,
            int y,
            int z,
            Voxel voxel)
        {
            if (chunk == null)
            {
                return;
            }

            if (!VoxelIndex.IsValidLocalCoordinate(x, y, z))
            {
                return;
            }

            int index = VoxelIndex.ToIndex(x, y, z);

            NativeArraySet(
                chunk.Voxels,
                index,
                voxel
            );
        }

        public static ushort GetBlockId(
            ChunkData chunk,
            int x,
            int y,
            int z)
        {
            if (chunk == null)
            {
                return BlockIds.Air;
            }

            if (!VoxelIndex.IsValidLocalCoordinate(x, y, z))
            {
                return BlockIds.Air;
            }

            return chunk.Voxels[
                VoxelIndex.ToIndex(x, y, z)
            ].BlockId;
        }

        private static void NativeArraySet(
            Unity.Collections.NativeArray<Voxel> array,
            int index,
            Voxel value)
        {
            array[index] = value;
        }
    }
}
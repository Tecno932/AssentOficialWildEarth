namespace WildEarth.Voxel
{
    public static class FluidChunkUtility
    {
        public static bool ContainsFluids(
            ChunkData chunkData,
            FluidRuntimeDatabase fluidDatabase)
        {
            if (chunkData == null)
                return false;

            if (!chunkData.IsCreated)
                return false;

            if (fluidDatabase == null)
                return false;

            if (!fluidDatabase.IsCreated)
                return false;

            var fluidsByBlockId =
                fluidDatabase.AsBlockLookupNativeArray();

            for (
                int index = 0;
                index < VoxelConstants.VoxelsPerChunk;
                index++)
            {
                Voxel voxel =
                    chunkData.Voxels[index];

                if (voxel.IsAir ||
                    voxel.State == 0)
                {
                    continue;
                }

                ushort blockId =
                    voxel.BlockId;

                if (blockId >= fluidsByBlockId.Length)
                    continue;

                FluidRuntimeData fluid =
                    fluidsByBlockId[blockId];

                if (fluid.IsValid)
                    return true;
            }

            return false;
        }
    }
}
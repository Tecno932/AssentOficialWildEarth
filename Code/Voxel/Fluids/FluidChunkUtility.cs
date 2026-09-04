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

            for (
                int index = 0;
                index < VoxelConstants.VoxelsPerChunk;
                index++)
            {
                Voxel voxel = chunkData.Voxels[index];

                if (voxel.IsAir)
                    continue;

                if (
                    fluidDatabase.TryGetByBlockId(
                        voxel.BlockId,
                        out FluidRuntimeData fluid
                    ))
                {
                    if (voxel.State > 0)
                        return true;
                }
            }

            return false;
        }
    }
}
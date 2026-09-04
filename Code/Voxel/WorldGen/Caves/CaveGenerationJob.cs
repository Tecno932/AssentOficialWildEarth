using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    [BurstCompile]
    public struct CaveGenerationJob : IJob
    {
        public ChunkGenerationContext Context;
        public CaveGenerationSettings Settings;

        public NativeArray<Voxel> Voxels;

        public NativeArray<BlockRuntimeData> BlockDatabase;

        public void Execute()
        {
            if (!Settings.Enabled)
                return;

            int chunkSize =
                VoxelConstants.ChunkSize;

            for (int y = 0; y < chunkSize; y++)
            {
                int worldY =
                    Context.WorldOrigin.y + y;

                if (worldY < Settings.MinimumY ||
                    worldY > Settings.MaximumY)
                {
                    continue;
                }

                for (int z = 0; z < chunkSize; z++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        int index =
                            VoxelIndex.ToIndex(
                                x,
                                y,
                                z
                            );

                        Voxel voxel =
                            Voxels[index];

                        if (voxel.BlockId ==
                            BlockIds.Air)
                        {
                            continue;
                        }

                        if (!CanCarve(
                                voxel.BlockId))
                        {
                            continue;
                        }

                        int worldX =
                            Context.WorldOrigin.x + x;

                        int worldZ =
                            Context.WorldOrigin.z + z;

                        float3 position =
                            new float3(
                                worldX,
                                worldY,
                                worldZ
                            );

                        float density =
                            CaveNoise.Fractal01(
                                position,
                                Settings.Frequency,
                                Settings.Octaves,
                                Settings.Lacunarity,
                                Settings.Persistence,
                                Context.Seed +
                                Settings.SeedOffset
                            );

                        if (density >=
                            Settings.Threshold)
                        {
                            Voxels[index] =
                                new Voxel(
                                    BlockIds.Air
                                );
                        }
                    }
                }
            }
        }

        private bool CanCarve(
            ushort blockId)
        {
            int index = blockId;

            if (index < 0 ||
                index >= BlockDatabase.Length)
            {
                return false;
            }

            BlockRuntimeData block =
                BlockDatabase[index];

            return block.Id == blockId &&
                   block.IsCaveCarvable;
        }
    }
}
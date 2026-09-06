using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace WildEarth.Voxel
{
    [BurstCompile]
    public struct FluidGenerationJob : IJob
    {
        public FluidGenerationSettings Settings;
        public TerrainGenerationSettings TerrainSettings;

        public NativeArray<Voxel> Voxels;
        public NativeArray<int> SurfaceHeights;

        public FluidRuntimeData Water;

        public void Execute()
        {
            if (!Settings.Enabled ||
                !Settings.GenerateWater)
            {
                return;
            }

            int chunkSize =
                VoxelConstants.ChunkSize;

            for (int z = 0; z < chunkSize; z++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    int surfaceIndex =
                        x +
                        z * chunkSize;

                    int terrainHeight =
                        SurfaceHeights[surfaceIndex];

                    if (terrainHeight >=
                        TerrainSettings.SeaLevel)
                    {
                        continue;
                    }

                    int startY =
                        terrainHeight + 1;

                    int endY =
                        TerrainSettings.SeaLevel;

                    for (int worldY = startY;
                         worldY <= endY;
                         worldY++)
                    {
                        if (worldY <
                            VoxelConstants.MinVoxelY ||
                            worldY >
                            VoxelConstants.MaxVoxelY)
                        {
                            continue;
                        }

                        int localY =
                            worldY -
                            WorldOriginY;

                        if (localY < 0 ||
                            localY >= chunkSize)
                        {
                            continue;
                        }

                        int voxelIndex =
                            VoxelIndex.ToIndex(
                                x,
                                localY,
                                z
                            );

                        Voxel voxel =
                            Voxels[voxelIndex];

                        if (!voxel.IsAir)
                            continue;

                        voxel.BlockId =
                            Water.BlockId;

                        voxel.State =
                            Water.MaxLevel;

                        Voxels[voxelIndex] =
                            voxel;
                    }
                }
            }
        }

        public int WorldOriginY;
    }
}
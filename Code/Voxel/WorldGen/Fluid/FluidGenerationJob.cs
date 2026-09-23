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

        [ReadOnly]
        public NativeArray<int> SurfaceHeights;

        public FluidRuntimeData Water;

        public int WorldOriginY;

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

                    int chunkMinY =
                        WorldOriginY;

                    int chunkMaxY =
                        WorldOriginY +
                        chunkSize -
                        1;

                    int fillMinY =
                        startY > chunkMinY
                            ? startY
                            : chunkMinY;

                    int fillMaxY =
                        endY < chunkMaxY
                            ? endY
                            : chunkMaxY;

                    if (fillMinY > fillMaxY)
                        continue;

                    for (int worldY = fillMinY;
                        worldY <= fillMaxY;
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
    }
}
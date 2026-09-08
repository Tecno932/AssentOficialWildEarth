using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

using VoxelData = WildEarth.Voxel.Voxel;
using WildEarth.Voxel;

namespace WildEarth.Tests.Voxel.FluidTests
{
    public sealed class FluidGenerationJobTests
    {
        [Test]
        public void FluidGenerationJob_LowTerrain_FillsWaterToSeaLevel()
        {
            NativeArray<VoxelData> voxels =
                new NativeArray<VoxelData>(
                    WildEarth.Voxel.VoxelConstants.VoxelsPerChunk,
                    Allocator.TempJob
                );

            NativeArray<int> surfaceHeights =
                new NativeArray<int>(
                    WildEarth.Voxel.VoxelConstants.ChunkSize *
                    WildEarth.Voxel.VoxelConstants.ChunkSize,
                    Allocator.TempJob
                );

            try
            {
                for (int i = 0;
                     i < voxels.Length;
                     i++)
                {
                    voxels[i] =
                        new VoxelData(
                            WildEarth.Voxel.BlockIds.Air
                        );
                }

                for (int i = 0;
                     i < surfaceHeights.Length;
                     i++)
                {
                    surfaceHeights[i] = 4;
                }

                WildEarth.Voxel.FluidGenerationJob job =
                    new WildEarth.Voxel.FluidGenerationJob
                    {
                        Settings =
                            WildEarth.Voxel.FluidGenerationSettings.Default,

                        TerrainSettings =
                            WildEarth.Voxel.TerrainGenerationSettings.Default,

                        Voxels = voxels,

                        SurfaceHeights =
                            surfaceHeights,

                        Water =
                            new WildEarth.Voxel.FluidRuntimeData
                            {
                                Type =
                                    WildEarth.Voxel.FluidType.Water,

                                BlockId = 7,

                                MaxLevel = 15,

                                HorizontalFlowDecay = 1,

                                VerticalFlowDecay = 0,

                                IsLava = false
                            },

                        WorldOriginY = 0
                    };

                Unity.Jobs.JobHandle handle =
                    job.Schedule();

                handle.Complete();

                int chunkSize =
                    WildEarth.Voxel.VoxelConstants.ChunkSize;

                for (int z = 0;
                     z < chunkSize;
                     z++)
                {
                    for (int x = 0;
                         x < chunkSize;
                         x++)
                    {
                        for (int y = 0;
                             y < chunkSize;
                             y++)
                        {
                            VoxelData voxel =
                                voxels[
                                    WildEarth.Voxel.VoxelIndex.ToIndex(
                                        x,
                                        y,
                                        z
                                    )
                                ];

                            if (y >= 5 && y <= 15)
                            {
                                Assert.That(
                                    voxel.BlockId,
                                    Is.EqualTo(7),
                                    $"Se esperaba agua en ({x}, {y}, {z})."
                                );

                                Assert.That(
                                    voxel.State,
                                    Is.EqualTo(15),
                                    $"Se esperaba nivel 15 en ({x}, {y}, {z})."
                                );
                            }
                            else
                            {
                                Assert.That(
                                    voxel.BlockId,
                                    Is.EqualTo(
                                        WildEarth.Voxel.BlockIds.Air
                                    ),
                                    $"No se esperaba agua en ({x}, {y}, {z})."
                                );
                            }
                        }
                    }
                }
            }
            finally
            {
                if (voxels.IsCreated)
                    voxels.Dispose();

                if (surfaceHeights.IsCreated)
                    surfaceHeights.Dispose();
            }
        }
    }
}
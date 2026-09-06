using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    [BurstCompile]
    public struct TerrainGenerationJob : IJob
    {
        public ChunkGenerationContext Context;
        public TerrainGenerationSettings Settings;

        public NativeArray<Voxel> Voxels;

        public NativeArray<BiomeId> Biomes;
        public NativeArray<BiomeRuntimeData> BiomeDatabase;

        public NativeArray<int> SurfaceHeights;

        public void Execute()
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            for (int z = 0; z < chunkSize; z++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    BiomeId biomeId =
                        GetBiome(
                            x,
                            z
                        );

                    BiomeRuntimeData biome =
                        GetBiomeData(
                            biomeId
                        );

                    int worldX =
                        Context.WorldOrigin.x + x;

                    int worldZ =
                        Context.WorldOrigin.z + z;

                    int terrainHeight =
                        CalculateTerrainHeight(
                            worldX,
                            worldZ,
                            biome
                        );

                    int surfaceIndex =
                        x +
                        z * chunkSize;

                    SurfaceHeights[surfaceIndex] =
                        terrainHeight;

                    for (int y = 0; y < chunkSize; y++)
                    {
                        int worldY =
                            Context.WorldOrigin.y + y;

                        ushort blockId =
                            ResolveBlock(
                                worldY,
                                terrainHeight,
                                biome
                            );

                        int index =
                            VoxelIndex.ToIndex(
                                x,
                                y,
                                z
                            );

                        Voxels[index] =
                            new Voxel(
                                blockId
                            );
                    }
                }
            }
        }

        private BiomeId GetBiome(
            int localX,
            int localZ)
        {
            int index =
                localX +
                localZ *
                VoxelConstants.ChunkSize;

            return Biomes[index];
        }

        private BiomeRuntimeData GetBiomeData(
            BiomeId biomeId)
        {
            int index =
                (int)biomeId;

            if (index < 0 ||
                index >= BiomeDatabase.Length)
            {
                return CreateFallbackBiome();
            }

            return BiomeDatabase[index];
        }

        private BiomeRuntimeData CreateFallbackBiome()
        {
            return new BiomeRuntimeData
            {
                Id = BiomeId.Plains,

                TemperatureMin = 0f,
                TemperatureMax = 1f,

                MoistureMin = 0f,
                MoistureMax = 1f,

                TerrainHeightMultiplier = 1f,
                TerrainHeightOffset = 0f,

                SurfaceBlockId =
                    Settings.GrassBlockId,

                SubSurfaceBlockId =
                    Settings.DirtBlockId,

                DeepBlockId =
                    Settings.StoneBlockId,

                SubSurfaceDepth = 3
            };
        }

        private int CalculateTerrainHeight(
            int worldX,
            int worldZ,
            BiomeRuntimeData biome)
        {
            float2 position =
                new float2(
                    worldX,
                    worldZ
                );

            float continentalness =
                TerrainNoise.Sample01(
                    position,
                    Settings.ContinentalFrequency,
                    Context.Seed + 1000
                );

            float continentalShape =
                continentalness * 2f - 1f;

            float height =
                Settings.BaseHeight;

            height +=
                continentalShape *
                Settings.ContinentalAmplitude;

            float erosion =
                TerrainNoise.Fractal01(
                    position,
                    Settings.ErosionFrequency,
                    octaves: 3,
                    lacunarity: 2f,
                    persistence: 0.5f,
                    seed: Context.Seed + 2000
                );

            float erosionShape =
                erosion * 2f - 1f;

            height +=
                erosionShape *
                Settings.ErosionAmplitude;

            float peaks =
                TerrainNoise.Fractal01(
                    position,
                    Settings.PeaksFrequency,
                    octaves: 4,
                    lacunarity: 2f,
                    persistence: 0.5f,
                    seed: Context.Seed + 3000
                );

            float peaksShape =
                math.pow(
                    peaks,
                    1.75f
                );

            height +=
                peaksShape *
                Settings.PeaksAmplitude;

            float detail =
                TerrainNoise.Sample(
                    position,
                    Settings.DetailFrequency,
                    Context.Seed + 4000
                );

            height +=
                detail *
                Settings.DetailAmplitude;

            height =
                Settings.BaseHeight +
                (
                    height -
                    Settings.BaseHeight
                ) *
                math.max(
                    0f,
                    biome.TerrainHeightMultiplier
                ) *
                Settings.BiomeHeightInfluence;

            height +=
                biome.TerrainHeightOffset;

            height =
                math.clamp(
                    height,
                    VoxelConstants.MinVoxelY,
                    VoxelConstants.MaxVoxelY
                );

            return (int)math.round(height);
        }

        private ushort ResolveBlock(
            int worldY,
            int terrainHeight,
            BiomeRuntimeData biome)
        {
            if (worldY > terrainHeight)
                return BlockIds.Air;

            if (worldY == terrainHeight)
                return biome.SurfaceBlockId;

            int depth =
                terrainHeight -
                worldY;

            if (depth <= biome.SubSurfaceDepth)
                return biome.SubSurfaceBlockId;

            return biome.DeepBlockId;
        }
    }
}

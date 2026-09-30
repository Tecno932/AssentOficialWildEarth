using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    [BurstCompile]
    public struct BiomeGenerationJob : IJob
    {
        public ChunkGenerationContext Context;
        public BiomeGenerationSettings Settings;

        [ReadOnly]
        public NativeArray<BiomeRuntimeData> BiomeDatabase;

        public NativeArray<BiomeId> Output;

        private struct RegionSample
        {
            public int CellX;
            public int CellZ;

            public float2 Center;

            public float Temperature;
            public float Moisture;
        }

        public void Execute()
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            float regionSize =
                math.max(
                    64f,
                    Settings.BiomeRegionSize
                );

            float2 temperatureSeedOffset =
                new float2(
                    Context.Seed * 0.371f,
                    Context.Seed * 0.619f
                );

            int moistureSeed =
                Context.Seed +
                Settings.BiomeSeedOffset;

            float2 moistureSeedOffset =
                new float2(
                    moistureSeed * 0.271f,
                    moistureSeed * 0.733f
                );

            int minCellX =
                (int)math.floor(
                    Context.WorldOrigin.x /
                    regionSize
                );

            int maxCellX =
                (int)math.floor(
                    (Context.WorldOrigin.x +
                     chunkSize - 1) /
                    regionSize
                );

            int minCellZ =
                (int)math.floor(
                    Context.WorldOrigin.z /
                    regionSize
                );

            int maxCellZ =
                (int)math.floor(
                    (Context.WorldOrigin.z +
                     chunkSize - 1) /
                    regionSize
                );

            int cellWidth =
                maxCellX -
                minCellX +
                3;

            int cellHeight =
                maxCellZ -
                minCellZ +
                3;

            int cacheOriginX =
                minCellX - 1;

            int cacheOriginZ =
                minCellZ - 1;

            NativeArray<RegionSample> regionCache =
                new NativeArray<RegionSample>(
                    cellWidth * cellHeight,
                    Allocator.Temp
                );

            for (int cacheZ = 0;
                 cacheZ < cellHeight;
                 cacheZ++)
            {
                int cellZ =
                    cacheOriginZ + cacheZ;

                for (int cacheX = 0;
                     cacheX < cellWidth;
                     cacheX++)
                {
                    int cellX =
                        cacheOriginX + cacheX;

                    float2 center =
                        GetRegionCenter(
                            cellX,
                            cellZ,
                            regionSize
                        );

                    float temperature =
                        CalculateTemperature(
                            center.x,
                            center.y,
                            temperatureSeedOffset
                        );

                    float moisture =
                        CalculateMoisture(
                            center.x,
                            center.y,
                            moistureSeedOffset
                        );

                    int cacheIndex =
                        cacheX +
                        cacheZ * cellWidth;

                    regionCache[cacheIndex] =
                        new RegionSample
                        {
                            CellX = cellX,
                            CellZ = cellZ,
                            Center = center,
                            Temperature = temperature,
                            Moisture = moisture
                        };
                }
            }

            for (int z = 0;
                 z < chunkSize;
                 z++)
            {
                int worldZ =
                    Context.WorldOrigin.z + z;

                int cellZ =
                    (int)math.floor(
                        worldZ /
                        regionSize
                    );

                for (int x = 0;
                     x < chunkSize;
                     x++)
                {
                    int worldX =
                        Context.WorldOrigin.x + x;

                    int cellX =
                        (int)math.floor(
                            worldX /
                            regionSize
                        );

                    float2 position =
                        new float2(
                            worldX,
                            worldZ
                        );

                    float bestDistance =
                        float.MaxValue;

                    float bestTemperature = 0f;
                    float bestMoisture = 0f;

                    /*
                     * Preserve the original candidate order:
                     * offsetZ -> offsetX.
                     *
                     * This keeps tie-breaking deterministic.
                     */
                    for (int offsetZ = -1;
                         offsetZ <= 1;
                         offsetZ++)
                    {
                        int candidateZ =
                            cellZ + offsetZ;

                        int cacheZ =
                            candidateZ -
                            cacheOriginZ;

                        for (int offsetX = -1;
                             offsetX <= 1;
                             offsetX++)
                        {
                            int candidateX =
                                cellX + offsetX;

                            int cacheX =
                                candidateX -
                                cacheOriginX;

                            int cacheIndex =
                                cacheX +
                                cacheZ * cellWidth;

                            RegionSample sample =
                                regionCache[
                                    cacheIndex
                                ];

                            float2 difference =
                                position -
                                sample.Center;

                            float distance =
                                math.lengthsq(
                                    difference
                                );

                            if (distance >= bestDistance)
                                continue;

                            bestDistance =
                                distance;

                            bestTemperature =
                                sample.Temperature;

                            bestMoisture =
                                sample.Moisture;
                        }
                    }

                    BiomeId biome =
                        BiomeSelector.Select(
                            BiomeDatabase,
                            bestTemperature,
                            bestMoisture
                        );

                    int outputIndex =
                        x +
                        z * chunkSize;

                    Output[outputIndex] =
                        biome;
                }
            }

            regionCache.Dispose();
        }

        private float2 GetRegionCenter(
            int cellX,
            int cellZ,
            float regionSize)
        {
            uint seed =
                Hash(
                    cellX,
                    cellZ,
                    Context.Seed +
                    Settings.BiomeSeedOffset
                );

            float randomX =
                HashTo01(
                    seed
                );

            float randomZ =
                HashTo01(
                    seed ^ 0x9E3779B9u
                );

            float jitter =
                math.clamp(
                    Settings.BiomeRegionJitter,
                    0f,
                    0.45f
                );

            float offsetX =
                (randomX - 0.5f) *
                regionSize *
                2f *
                jitter;

            float offsetZ =
                (randomZ - 0.5f) *
                regionSize *
                2f *
                jitter;

            return new float2(
                cellX * regionSize +
                regionSize * 0.5f +
                offsetX,

                cellZ * regionSize +
                regionSize * 0.5f +
                offsetZ
            );
        }

        private float CalculateTemperature(
            float worldX,
            float worldZ,
            float2 seedOffset)
        {
            float2 position =
                new float2(
                    worldX,
                    worldZ
                );

            position *=
                Settings.TemperatureFrequency;

            float value =
                noise.snoise(
                    position +
                    seedOffset
                );

            return
                (value + 1f) * 0.5f;
        }

        private float CalculateMoisture(
            float worldX,
            float worldZ,
            float2 seedOffset)
        {
            float2 position =
                new float2(
                    worldX,
                    worldZ
                );

            position *=
                Settings.MoistureFrequency;

            float value =
                noise.snoise(
                    position +
                    seedOffset
                );

            return
                (value + 1f) * 0.5f;
        }

        private static uint Hash(
            int x,
            int z,
            int seed)
        {
            uint h =
                (uint)seed;

            h ^= (uint)x *
                374761393u;

            h ^= (uint)z *
                668265263u;

            h ^= h >> 13;

            h *=
                1274126177u;

            h ^= h >> 16;

            return h;
        }

        private static float HashTo01(
            uint value)
        {
            return
                (value & 0x00FFFFFFu) /
                16777215f;
        }
    }
}
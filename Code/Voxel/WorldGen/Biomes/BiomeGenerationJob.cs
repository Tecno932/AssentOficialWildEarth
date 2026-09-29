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

        public void Execute()
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

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

            for (int z = 0;
                 z < chunkSize;
                 z++)
            {
                for (int x = 0;
                     x < chunkSize;
                     x++)
                {
                    int worldX =
                        Context.WorldOrigin.x + x;

                    int worldZ =
                        Context.WorldOrigin.z + z;

                    BiomeId biome =
                        CalculateBiome(
                            worldX,
                            worldZ,
                            temperatureSeedOffset,
                            moistureSeedOffset
                        );

                    int index =
                        x +
                        z * chunkSize;

                    Output[index] =
                        biome;
                }
            }
        }

        private BiomeId CalculateBiome(
            int worldX,
            int worldZ,
            float2 temperatureSeedOffset,
            float2 moistureSeedOffset)
        {
            float regionSize =
                math.max(
                    64f,
                    Settings.BiomeRegionSize
                );

            int cellX =
                (int)math.floor(
                    worldX / regionSize
                );

            int cellZ =
                (int)math.floor(
                    worldZ / regionSize
                );

            float2 position =
                new float2(
                    worldX,
                    worldZ
                );

            float bestDistance =
                float.MaxValue;

            int bestCellX = cellX;
            int bestCellZ = cellZ;

            for (int offsetZ = -1;
                 offsetZ <= 1;
                 offsetZ++)
            {
                for (int offsetX = -1;
                     offsetX <= 1;
                     offsetX++)
                {
                    int candidateX =
                        cellX + offsetX;

                    int candidateZ =
                        cellZ + offsetZ;

                    float2 center =
                        GetRegionCenter(
                            candidateX,
                            candidateZ,
                            regionSize
                        );

                    float2 difference =
                        position - center;

                    float distance =
                        math.lengthsq(
                            difference
                        );

                    if (distance >= bestDistance)
                        continue;

                    bestDistance =
                        distance;

                    bestCellX =
                        candidateX;

                    bestCellZ =
                        candidateZ;
                }
            }

            float2 selectedCenter =
                GetRegionCenter(
                    bestCellX,
                    bestCellZ,
                    regionSize
                );

            float temperature =
                CalculateTemperature(
                    selectedCenter.x,
                    selectedCenter.y,
                    temperatureSeedOffset
                );

            float moisture =
                CalculateMoisture(
                    selectedCenter.x,
                    selectedCenter.y,
                    moistureSeedOffset
                );

            return BiomeSelector.Select(
                BiomeDatabase,
                temperature,
                moisture
            );
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
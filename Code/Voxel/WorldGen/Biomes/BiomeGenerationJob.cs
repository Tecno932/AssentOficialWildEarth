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

                    float temperature =
                        CalculateTemperature(
                            worldX,
                            worldZ,
                            temperatureSeedOffset
                        );

                    float moisture =
                        CalculateMoisture(
                            worldX,
                            worldZ,
                            moistureSeedOffset
                        );

                    BiomeId biome =
                        BiomeSelector.Select(
                            BiomeDatabase,
                            temperature,
                            moisture
                        );

                    int index =
                        x +
                        z * chunkSize;

                    Output[index] =
                        biome;
                }
            }
        }

        private float CalculateTemperature(
            int worldX,
            int worldZ,
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
            int worldX,
            int worldZ,
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
    }
}
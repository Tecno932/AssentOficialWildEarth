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

        public NativeArray<BiomeRuntimeData> BiomeDatabase;
        public NativeArray<BiomeId> Output;

        public void Execute()
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            for (int z = 0; z < chunkSize; z++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    int3 worldPosition =
                        Context.GetWorldPosition(
                            x,
                            0,
                            z
                        );

                    float temperature =
                        CalculateTemperature(
                            worldPosition.x,
                            worldPosition.z
                        );

                    float moisture =
                        CalculateMoisture(
                            worldPosition.x,
                            worldPosition.z
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
            int worldZ)
        {
            float2 position =
                new float2(
                    worldX,
                    worldZ
                );

            position *=
                Settings.TemperatureFrequency;

            float2 seedOffset =
                new float2(
                    Context.Seed * 0.371f,
                    Context.Seed * 0.619f
                );

            float value =
                noise.snoise(
                    position + seedOffset
                );

            return
                (value + 1f) * 0.5f;
        }

        private float CalculateMoisture(
            int worldX,
            int worldZ)
        {
            float2 position =
                new float2(
                    worldX,
                    worldZ
                );

            position *=
                Settings.MoistureFrequency;

            float2 seedOffset =
                new float2(
                    (Context.Seed +
                     Settings.BiomeSeedOffset) * 0.271f,

                    (Context.Seed +
                     Settings.BiomeSeedOffset) * 0.733f
                );

            float value =
                noise.snoise(
                    position + seedOffset
                );

            return
                (value + 1f) * 0.5f;
        }
    }
}
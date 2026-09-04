using Unity.Collections;

namespace WildEarth.Voxel
{
    public static class BiomeSelector
    {
        public static BiomeId Select(
            NativeArray<BiomeRuntimeData> biomes,
            float temperature,
            float moisture)
        {
            if (!biomes.IsCreated ||
                biomes.Length == 0)
            {
                return BiomeId.Plains;
            }

            BiomeId bestBiome =
                BiomeId.Plains;

            float bestScore =
                float.MaxValue;

            for (int i = 0; i < biomes.Length; i++)
            {
                BiomeRuntimeData biome =
                    biomes[i];

                if (!biome.MatchesClimate(
                        temperature,
                        moisture))
                {
                    continue;
                }

                float temperatureCenter =
                    (biome.TemperatureMin +
                     biome.TemperatureMax) *
                    0.5f;

                float moistureCenter =
                    (biome.MoistureMin +
                     biome.MoistureMax) *
                    0.5f;

                float temperatureDistance =
                    temperature -
                    temperatureCenter;

                float moistureDistance =
                    moisture -
                    moistureCenter;

                float score =
                    temperatureDistance *
                    temperatureDistance +
                    moistureDistance *
                    moistureDistance;

                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestBiome = biome.Id;
            }

            return bestBiome;
        }
    }
}
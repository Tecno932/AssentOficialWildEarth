using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct BiomeGenerationSettings
    {
        public float TemperatureFrequency;
        public float MoistureFrequency;

        public float BiomeRegionSize;
        public float BiomeRegionJitter;

        public int BiomeSeedOffset;

        public static BiomeGenerationSettings Default =>
            new BiomeGenerationSettings
            {
                TemperatureFrequency = 0.0008f,
                MoistureFrequency = 0.0008f,

                BiomeRegionSize = 420f,
                BiomeRegionJitter = 0.32f,

                BiomeSeedOffset = 10000
            };
    }
}
using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct BiomeGenerationSettings
    {
        public float TemperatureFrequency;
        public float MoistureFrequency;

        public int BiomeSeedOffset;

        public static BiomeGenerationSettings Default =>
            new BiomeGenerationSettings
            {
                TemperatureFrequency = 0.0015f,
                MoistureFrequency = 0.0015f,
                BiomeSeedOffset = 10000
            };
    }
}
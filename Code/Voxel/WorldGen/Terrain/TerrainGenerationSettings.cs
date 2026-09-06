using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct TerrainGenerationSettings
    {
        public int BaseHeight;
        public int SeaLevel;
        public int TerrainAmplitude;

        public float ContinentalFrequency;
        public float ContinentalAmplitude;

        public float ErosionFrequency;
        public float ErosionAmplitude;

        public float PeaksFrequency;
        public float PeaksAmplitude;

        public float DetailFrequency;
        public float DetailAmplitude;

        public float BiomeHeightInfluence;

        public ushort StoneBlockId;
        public ushort DirtBlockId;
        public ushort GrassBlockId;

        public static TerrainGenerationSettings Default =>
            new TerrainGenerationSettings
            {
                BaseHeight = 64,
                SeaLevel = 40,
                TerrainAmplitude = 24,

                ContinentalFrequency = 0.0012f,
                ContinentalAmplitude = 32f,

                ErosionFrequency = 0.004f,
                ErosionAmplitude = 10f,

                PeaksFrequency = 0.0025f,
                PeaksAmplitude = 18f,

                DetailFrequency = 0.025f,
                DetailAmplitude = 2f,

                BiomeHeightInfluence = 1f,

                StoneBlockId = 1,
                DirtBlockId = 2,
                GrassBlockId = 3
            };
    }
}
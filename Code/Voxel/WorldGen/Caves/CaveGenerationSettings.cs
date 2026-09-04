using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct CaveGenerationSettings
    {
        public bool Enabled;

        public float Frequency;
        public int Octaves;

        public float Lacunarity;
        public float Persistence;

        public float Threshold;

        public int MinimumY;
        public int MaximumY;

        public int SeedOffset;

        public static CaveGenerationSettings Default =>
            new CaveGenerationSettings
            {
                Enabled = true,

                Frequency = 0.035f,
                Octaves = 3,

                Lacunarity = 2f,
                Persistence = 0.5f,

                Threshold = 0.62f,

                MinimumY = 5,
                MaximumY = 120,

                SeedOffset = 5000
            };
    }
}
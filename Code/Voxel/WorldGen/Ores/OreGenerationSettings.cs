using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct OreGenerationSettings
    {
        public bool Enabled;

        public int Octaves;

        public float Lacunarity;

        public float Persistence;

        public int SeedOffset;

        public static OreGenerationSettings Default =>
            new OreGenerationSettings
            {
                Enabled = true,

                Octaves = 3,

                Lacunarity = 2f,

                Persistence = 0.5f,

                SeedOffset = 7000
            };
    }
}
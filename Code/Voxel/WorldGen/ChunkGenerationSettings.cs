using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct ChunkGenerationSettings
    {
        public int Seed;

        public TerrainGenerationSettings Terrain;
        public BiomeGenerationSettings Biome;
        public CaveGenerationSettings Caves;
        public OreGenerationSettings Ores;
        public FluidGenerationSettings Fluids;

        public static ChunkGenerationSettings Default =>
            new ChunkGenerationSettings
            {
                Seed = 12345,

                Terrain =
                    TerrainGenerationSettings.Default,

                Biome =
                    BiomeGenerationSettings.Default,

                Caves =
                    CaveGenerationSettings.Default,

                Ores =
                    OreGenerationSettings.Default,

                Fluids =
                    FluidGenerationSettings.Default
            };
    }
}
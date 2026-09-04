namespace WildEarth.Voxel
{
    /// <summary>
    /// Etapas conceptuales del pipeline de generación.
    ///
    /// El orden es importante.
    /// </summary>
    public enum ChunkGenerationStage : byte
    {
        None = 0,

        Terrain = 1,
        Biome = 2,
        Caves = 3,
        Ores = 4,
        Fluids = 5,
        Structures = 6,
        Decoration = 7,

        Complete = 255
    }
}
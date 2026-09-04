namespace WildEarth.Voxel
{
    /// <summary>
    /// Estado principal del ciclo de vida de un chunk.
    ///
    /// Dirty/NeedsMesh/NeedsSave no forman parte de este enum;
    /// son flags independientes porque pueden coexistir.
    /// </summary>
    public enum ChunkState : byte
    {
        Unloaded = 0,
        Loading = 1,
        Generating = 2,
        Generated = 3,
        Meshing = 4,
        Ready = 5,
        Saving = 6,
        Unloading = 7,
        Failed = 8
    }
}
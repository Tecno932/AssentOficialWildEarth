using System;

namespace WildEarth.Voxel
{
    [Flags]
    public enum ChunkFlags : ushort
    {
        None = 0,

        Dirty = 1 << 0,
        NeedsMesh = 1 << 1,
        NeedsSave = 1 << 2,

        HasEntities = 1 << 3,
        HasBlockEntities = 1 << 4,

        Generated = 1 << 5,
        LoadedFromDisk = 1 << 6
    }
}
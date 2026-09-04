using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct OreRuntimeData
    {
        public ushort BlockId;

        public int MinY;
        public int MaxY;

        public float Rarity;

        public int MinVeinSize;
        public int MaxVeinSize;

        public float Frequency;

        public int HostBlockStart;
        public int HostBlockCount;

        public bool IsValid =>
            BlockId != BlockIds.Air &&
            MinY >= 0 &&
            MinY <= MaxY &&
            MinVeinSize > 0 &&
            MaxVeinSize >= MinVeinSize &&
            Rarity > 0f &&
            Frequency > 0f &&
            HostBlockCount > 0;
    }
}
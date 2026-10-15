using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct MaterialRuntimeData
    {
        public ushort Id;

        public MaterialStats Stats;

        public bool IsValid =>
            Id != 0;

        public float Hardness =>
            Stats.Hardness;

        public float Resistance =>
            Stats.Resistance;

        public float EdgeRetention =>
            Stats.EdgeRetention;
    }
}
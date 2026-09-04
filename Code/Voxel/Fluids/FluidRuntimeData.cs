using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct FluidRuntimeData
    {
        public FluidType Type;

        public ushort BlockId;

        public byte MaxLevel;
        public byte HorizontalFlowDecay;
        public byte VerticalFlowDecay;

        public bool IsLava;

        public bool IsValid =>
            Type != FluidType.None &&
            BlockId != BlockIds.Air &&
            MaxLevel > 0;
    }
}
using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct FluidSimulationState
    {
        public int X;
        public int Y;
        public int Z;

        public FluidState Fluid;

        public FluidSimulationState(
            int x,
            int y,
            int z,
            FluidState fluid)
        {
            X = x;
            Y = y;
            Z = z;
            Fluid = fluid;
        }

        public bool IsValid =>
            X >= 0 &&
            X < VoxelConstants.ChunkSize &&
            Y >= 0 &&
            Y < VoxelConstants.ChunkSize &&
            Z >= 0 &&
            Z < VoxelConstants.ChunkSize;

        public bool IsEmpty =>
            Fluid.IsEmpty;
    }
}
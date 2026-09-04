using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct FluidChange
    {
        public ChunkCoordinate TargetChunk;

        public int X;
        public int Y;
        public int Z;

        public FluidState State;

        public FluidChange(
            ChunkCoordinate targetChunk,
            int x,
            int y,
            int z,
            FluidState state)
        {
            TargetChunk = targetChunk;

            X = x;
            Y = y;
            Z = z;

            State = state;
        }

        public bool IsValid =>
            X >= 0 &&
            X < VoxelConstants.ChunkSize &&
            Y >= 0 &&
            Y < VoxelConstants.ChunkSize &&
            Z >= 0 &&
            Z < VoxelConstants.ChunkSize;

        public WorldVoxelCoordinate ToWorldCoordinate()
        {
            WorldVoxelCoordinate origin =
                TargetChunk.ToWorldOrigin();

            return new WorldVoxelCoordinate(
                origin.X + X,
                origin.Y + Y,
                origin.Z + Z
            );
        }
    }
}
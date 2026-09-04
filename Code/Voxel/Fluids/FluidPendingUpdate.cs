using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct FluidPendingUpdate : IEquatable<FluidPendingUpdate>
    {
        public ChunkCoordinate Chunk;

        public int X;
        public int Y;
        public int Z;

        public FluidState State;

        public int Distance;

        public FluidPendingUpdate(
            ChunkCoordinate chunk,
            int x,
            int y,
            int z,
            FluidState state,
            int distance = 0)
        {
            Chunk = chunk;

            X = x;
            Y = y;
            Z = z;

            State = state;

            Distance = distance;
        }

        public bool IsValid =>
            X >= 0 &&
            X < VoxelConstants.ChunkSize &&
            Y >= 0 &&
            Y < VoxelConstants.ChunkSize &&
            Z >= 0 &&
            Z < VoxelConstants.ChunkSize &&
            !State.IsEmpty;

        public FluidChange ToChange()
        {
            return new FluidChange(
                Chunk,
                X,
                Y,
                Z,
                State
            );
        }

        public FluidPendingUpdate Next(
            ChunkCoordinate chunk,
            int x,
            int y,
            int z,
            FluidState state)
        {
            return new FluidPendingUpdate(
                chunk,
                x,
                y,
                z,
                state,
                Distance + 1
            );
        }

        public bool Equals(
            FluidPendingUpdate other)
        {
            return Chunk == other.Chunk &&
                   X == other.X &&
                   Y == other.Y &&
                   Z == other.Z &&
                   State == other.State &&
                   Distance == other.Distance;
        }

        public override bool Equals(
            object obj)
        {
            return obj is FluidPendingUpdate other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Chunk,
                X,
                Y,
                Z,
                State,
                Distance
            );
        }

        public static bool operator ==(
            FluidPendingUpdate left,
            FluidPendingUpdate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            FluidPendingUpdate left,
            FluidPendingUpdate right)
        {
            return !left.Equals(right);
        }
    }
}
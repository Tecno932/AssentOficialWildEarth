using System;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    /// <summary>
    /// Coordenada de un chunk dentro del mundo.
    ///
    /// Un chunk representa una región de:
    /// 16 x 16 x 16 voxels.
    /// </summary>
    [Serializable]
    public readonly struct ChunkCoordinate : IEquatable<ChunkCoordinate>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public ChunkCoordinate(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public ChunkCoordinate(int3 value)
        {
            X = value.x;
            Y = value.y;
            Z = value.z;
        }

        public int3 ToInt3()
        {
            return new int3(X, Y, Z);
        }

        public WorldVoxelCoordinate ToWorldOrigin()
        {
            return new WorldVoxelCoordinate(
                X * VoxelConstants.ChunkSize,
                Y * VoxelConstants.ChunkSize,
                Z * VoxelConstants.ChunkSize
            );
        }

        public bool Equals(ChunkCoordinate other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is ChunkCoordinate other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public override string ToString()
        {
            return $"Chunk({X}, {Y}, {Z})";
        }

        public static bool operator ==(
            ChunkCoordinate left,
            ChunkCoordinate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            ChunkCoordinate left,
            ChunkCoordinate right)
        {
            return !left.Equals(right);
        }
    }
}
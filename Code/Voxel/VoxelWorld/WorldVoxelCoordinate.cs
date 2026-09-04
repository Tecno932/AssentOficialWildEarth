using System;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    /// <summary>
    /// Coordenada absoluta de un voxel dentro del mundo.
    /// Utiliza enteros para evitar errores de precisión de float.
    /// </summary>
    [Serializable]
    public readonly struct WorldVoxelCoordinate : IEquatable<WorldVoxelCoordinate>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public WorldVoxelCoordinate(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public WorldVoxelCoordinate(int3 value)
        {
            X = value.x;
            Y = value.y;
            Z = value.z;
        }

        public int3 ToInt3()
        {
            return new int3(X, Y, Z);
        }

        public bool Equals(WorldVoxelCoordinate other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldVoxelCoordinate other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public override string ToString()
        {
            return $"WorldVoxel({X}, {Y}, {Z})";
        }

        public static bool operator ==(
            WorldVoxelCoordinate left,
            WorldVoxelCoordinate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            WorldVoxelCoordinate left,
            WorldVoxelCoordinate right)
        {
            return !left.Equals(right);
        }

        public static WorldVoxelCoordinate operator +(
            WorldVoxelCoordinate left,
            WorldVoxelCoordinate right)
        {
            return new WorldVoxelCoordinate(
                left.X + right.X,
                left.Y + right.Y,
                left.Z + right.Z
            );
        }

        public static WorldVoxelCoordinate operator -(
            WorldVoxelCoordinate left,
            WorldVoxelCoordinate right)
        {
            return new WorldVoxelCoordinate(
                left.X - right.X,
                left.Y - right.Y,
                left.Z - right.Z
            );
        }
    }
}
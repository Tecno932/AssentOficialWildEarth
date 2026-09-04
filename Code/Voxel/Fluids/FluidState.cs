using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct FluidState : IEquatable<FluidState>
    {
        public const byte MaxLevel = 15;

        public FluidType Type;
        public byte Level;

        public bool IsEmpty =>
            Type == FluidType.None ||
            Level == 0;

        public bool IsSource =>
            !IsEmpty &&
            Level == MaxLevel;

        public FluidState(
            FluidType type,
            byte level)
        {
            Type = type;

            Level = level > MaxLevel
                ? MaxLevel
                : level;

            if (Level == 0)
                Type = FluidType.None;
        }

        public static FluidState Empty =>
            new FluidState(
                FluidType.None,
                0
            );

        public static FluidState Source(
            FluidType type)
        {
            if (type == FluidType.None)
                return Empty;

            return new FluidState(
                type,
                MaxLevel
            );
        }

        public FluidState WithLevel(
            byte level)
        {
            return new FluidState(
                Type,
                level
            );
        }

        public bool Equals(
            FluidState other)
        {
            return Type == other.Type &&
                   Level == other.Level;
        }

        public override bool Equals(
            object obj)
        {
            return obj is FluidState other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Type,
                Level
            );
        }

        public static bool operator ==(
            FluidState left,
            FluidState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            FluidState left,
            FluidState right)
        {
            return !left.Equals(right);
        }
    }
}
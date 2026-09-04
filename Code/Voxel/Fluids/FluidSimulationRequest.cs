using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public readonly struct FluidSimulationRequest :
        IEquatable<FluidSimulationRequest>
    {
        public ChunkCoordinate Chunk { get; }

        public FluidSimulationRequest(
            ChunkCoordinate chunk)
        {
            Chunk = chunk;
        }

        public bool Equals(
            FluidSimulationRequest other)
        {
            return Chunk == other.Chunk;
        }

        public override bool Equals(
            object obj)
        {
            return obj is FluidSimulationRequest other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            return Chunk.GetHashCode();
        }

        public override string ToString()
        {
            return $"FluidSimulationRequest({Chunk})";
        }

        public static bool operator ==(
            FluidSimulationRequest left,
            FluidSimulationRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            FluidSimulationRequest left,
            FluidSimulationRequest right)
        {
            return !left.Equals(right);
        }
    }
}
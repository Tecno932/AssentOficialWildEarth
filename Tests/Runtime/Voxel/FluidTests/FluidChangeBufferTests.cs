using NUnit.Framework;
using Unity.Collections;

namespace WildEarth.Voxel.Tests
{
    public sealed class FluidChangeBufferTests
    {
        [Test]
        public void CapacityMatchesVoxelCount()
        {
            using FluidChangeBuffer buffer =
                new FluidChangeBuffer(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Temp
                );

            Assert.AreEqual(
                VoxelConstants.VoxelsPerChunk *
                FluidChangeBuffer.MaxChangesPerVoxel,
                buffer.Capacity
            );
        }

        [Test]
        public void CountsStartAtZero()
        {
            using FluidChangeBuffer buffer =
                new FluidChangeBuffer(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Temp
                );

            Assert.AreEqual(
                0,
                buffer.GetChangeCount(0)
            );

            Assert.AreEqual(
                0,
                buffer.GetChangeCount(
                    VoxelConstants.VoxelsPerChunk - 1
                )
            );
        }

        [Test]
        public void StoredChangeCanBeRead()
        {
            using FluidChangeBuffer buffer =
                new FluidChangeBuffer(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Temp
                );

            NativeArray<FluidChange> changes =
                buffer.AsNativeArray();

            NativeArray<byte> counts =
                buffer.AsCountArray();

            int voxelIndex = 100;

            FluidState state =
                new FluidState(
                    FluidType.Water,
                    12
                );

            FluidChange expected =
                new FluidChange(
                    new ChunkCoordinate(
                        0,
                        0,
                        0
                    ),
                    4,
                    7,
                    9,
                    state
                );

            int outputIndex =
                voxelIndex *
                FluidChangeBuffer.MaxChangesPerVoxel;

            changes[outputIndex] = expected;
            counts[voxelIndex] = 1;

            FluidChange actual =
                buffer.GetChange(
                    voxelIndex,
                    0
                );

            Assert.AreEqual(
                expected.TargetChunk,
                actual.TargetChunk
            );

            Assert.AreEqual(
                expected.X,
                actual.X
            );

            Assert.AreEqual(
                expected.Y,
                actual.Y
            );

            Assert.AreEqual(
                expected.Z,
                actual.Z
            );

            Assert.AreEqual(
                expected.State,
                actual.State
            );
        }

        [Test]
        public void ClearResetsAllCounts()
        {
            using FluidChangeBuffer buffer =
                new FluidChangeBuffer(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Temp
                );

            NativeArray<byte> counts =
                buffer.AsCountArray();

            counts[0] = 3;
            counts[100] = 2;
            counts[
                VoxelConstants.VoxelsPerChunk - 1
            ] = 5;

            buffer.Clear();

            Assert.AreEqual(
                0,
                buffer.GetChangeCount(0)
            );

            Assert.AreEqual(
                0,
                buffer.GetChangeCount(100)
            );

            Assert.AreEqual(
                0,
                buffer.GetChangeCount(
                    VoxelConstants.VoxelsPerChunk - 1
                )
            );
        }

        [Test]
        public void InvalidVoxelIndexThrows()
        {
            using FluidChangeBuffer buffer =
                new FluidChangeBuffer(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Temp
                );

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => buffer.GetChangeCount(-1)
            );

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => buffer.GetChangeCount(
                    VoxelConstants.VoxelsPerChunk
                )
            );
        }

        [Test]
        public void InvalidChangeIndexThrows()
        {
            using FluidChangeBuffer buffer =
                new FluidChangeBuffer(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Temp
                );

            NativeArray<byte> counts =
                buffer.AsCountArray();

            counts[0] = 1;

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => buffer.GetChange(0, -1)
            );

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => buffer.GetChange(0, 1)
            );
        }

        [Test]
        public void InvalidVoxelCountThrows()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () =>
                {
                    using FluidChangeBuffer buffer =
                        new FluidChangeBuffer(
                            0,
                            Allocator.Temp
                        );
                }
            );
        }
    }
}
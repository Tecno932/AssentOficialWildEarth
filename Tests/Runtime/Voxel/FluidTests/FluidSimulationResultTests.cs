using NUnit.Framework;
using Unity.Collections;

namespace WildEarth.Voxel.Tests
{
    public sealed class FluidSimulationResultTests
    {
        private FluidSimulationResult result;

        [SetUp]
        public void SetUp()
        {
            result =
                new FluidSimulationResult(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Persistent
                );
        }

        [TearDown]
        public void TearDown()
        {
            if (result != null)
                result.Dispose();
        }

        [Test]
        public void ResultIsCreated()
        {
            Assert.IsTrue(
                result.IsCreated
            );
        }

        [Test]
        public void VoxelCountMatchesChunk()
        {
            Assert.AreEqual(
                VoxelConstants.VoxelsPerChunk,
                result.VoxelCount
            );
        }

        [Test]
        public void ChangeCapacityMatchesExpectedCapacity()
        {
            Assert.AreEqual(
                VoxelConstants.VoxelsPerChunk *
                FluidChangeBuffer.MaxChangesPerVoxel,
                result.ChangeCapacity
            );
        }

        [Test]
        public void ChangesArrayIsCreated()
        {
            Assert.IsTrue(
                result.Changes.IsCreated
            );
        }

        [Test]
        public void ChangeCountsArrayIsCreated()
        {
            Assert.IsTrue(
                result.ChangeCounts.IsCreated
            );
        }

        [Test]
        public void NewResultHasZeroChanges()
        {
            for (
                int i = 0;
                i < result.VoxelCount;
                i++)
            {
                Assert.AreEqual(
                    0,
                    result.GetChangeCount(i)
                );
            }
        }

        [Test]
        public void GetChangeCountReturnsStoredCount()
        {
            int voxelIndex = 123;

            NativeArray<byte> changeCounts =
                result.ChangeCounts;

            changeCounts[voxelIndex] = 3;

            Assert.AreEqual(
                3,
                result.GetChangeCount(
                    voxelIndex
                )
            );
        }

        [Test]
        public void GetChangeReturnsStoredChange()
        {
            int voxelIndex = 42;

            FluidChange expected =
                new FluidChange(
                    new ChunkCoordinate(
                        2,
                        0,
                        -1
                    ),
                    4,
                    5,
                    6,
                    new FluidState(
                        FluidType.Water,
                        12
                    )
                );

            int index =
                voxelIndex *
                FluidChangeBuffer.MaxChangesPerVoxel;

            NativeArray<FluidChange> changes =
                result.Changes;

            NativeArray<byte> changeCounts =
                result.ChangeCounts;

            changes[index] = expected;
            changeCounts[voxelIndex] = 1;

            FluidChange actual =
                result.GetChange(
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
        public void GetChangeSupportsMultipleChangesPerVoxel()
        {
            int voxelIndex = 10;

            int baseIndex =
                voxelIndex *
                FluidChangeBuffer.MaxChangesPerVoxel;

            FluidChange first =
                new FluidChange(
                    new ChunkCoordinate(
                        0,
                        0,
                        0
                    ),
                    1,
                    2,
                    3,
                    new FluidState(
                        FluidType.Water,
                        15
                    )
                );

            FluidChange second =
                new FluidChange(
                    new ChunkCoordinate(
                        0,
                        0,
                        0
                    ),
                    4,
                    2,
                    3,
                    new FluidState(
                        FluidType.Water,
                        14
                    )
                );

            FluidChange third =
                new FluidChange(
                    new ChunkCoordinate(
                        0,
                        0,
                        0
                    ),
                    1,
                    2,
                    4,
                    new FluidState(
                        FluidType.Water,
                        13
                    )
                );

            NativeArray<FluidChange> changes =
                result.Changes;

            NativeArray<byte> changeCounts =
                result.ChangeCounts;

            changes[baseIndex] = first;
            changes[baseIndex + 1] = second;
            changes[baseIndex + 2] = third;

            changeCounts[voxelIndex] = 3;

            Assert.AreEqual(
                first,
                result.GetChange(
                    voxelIndex,
                    0
                )
            );

            Assert.AreEqual(
                second,
                result.GetChange(
                    voxelIndex,
                    1
                )
            );

            Assert.AreEqual(
                third,
                result.GetChange(
                    voxelIndex,
                    2
                )
            );
        }

        [Test]
        public void ClearRemovesChangeCounts()
        {
            NativeArray<byte> changeCounts =
                result.ChangeCounts;

            changeCounts[0] = 5;
            changeCounts[1] = 3;
            changeCounts[2] = 1;

            result.Clear();

            Assert.AreEqual(
                0,
                result.GetChangeCount(0)
            );

            Assert.AreEqual(
                0,
                result.GetChangeCount(1)
            );

            Assert.AreEqual(
                0,
                result.GetChangeCount(2)
            );
        }

        [Test]
        public void ClearDoesNotRequireClearingChangeData()
        {
            int voxelIndex = 5;

            int index =
                voxelIndex *
                FluidChangeBuffer.MaxChangesPerVoxel;

            NativeArray<FluidChange> changes =
                result.Changes;

            NativeArray<byte> changeCounts =
                result.ChangeCounts;

            changes[index] =
                new FluidChange(
                    new ChunkCoordinate(
                        1,
                        2,
                        3
                    ),
                    1,
                    1,
                    1,
                    new FluidState(
                        FluidType.Water,
                        10
                    )
                );

            changeCounts[voxelIndex] = 1;

            result.Clear();

            Assert.AreEqual(
                0,
                result.GetChangeCount(
                    voxelIndex
                )
            );
        }

        [Test]
        public void GetChangeRejectsInvalidVoxelIndex()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () =>
                    result.GetChangeCount(-1)
            );

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () =>
                    result.GetChangeCount(
                        result.VoxelCount
                    )
            );
        }

        [Test]
        public void GetChangeRejectsInvalidChangeIndex()
        {
            NativeArray<byte> changeCounts =
                result.ChangeCounts;

            changeCounts[0] = 1;

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () =>
                    result.GetChange(
                        0,
                        -1
                    )
            );

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () =>
                    result.GetChange(
                        0,
                        1
                    )
            );
        }

        [Test]
        public void GetChangeRejectsChangeWhenVoxelHasNoChanges()
        {
            Assert.AreEqual(
                0,
                result.GetChangeCount(0)
            );

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () =>
                    result.GetChange(
                        0,
                        0
                    )
            );
        }

        [Test]
        public void DisposeMakesResultNotCreated()
        {
            result.Dispose();

            Assert.IsFalse(
                result.IsCreated
            );
        }

        [Test]
        public void DisposeCanBeCalledTwice()
        {
            result.Dispose();
            result.Dispose();

            Assert.IsFalse(
                result.IsCreated
            );
        }

        [Test]
        public void ClearCanBeUsedAfterWritingChanges()
        {
            NativeArray<byte> changeCounts =
                result.ChangeCounts;

            for (int i = 0; i < 16; i++)
            {
                changeCounts[i] =
                    FluidChangeBuffer.MaxChangesPerVoxel;
            }

            result.Clear();

            for (int i = 0; i < 16; i++)
            {
                Assert.AreEqual(
                    0,
                    result.GetChangeCount(i)
                );
            }
        }
    }
}
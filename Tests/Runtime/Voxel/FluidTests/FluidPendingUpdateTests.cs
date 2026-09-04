using NUnit.Framework;

namespace WildEarth.Voxel.Tests
{
    public sealed class FluidPendingUpdateTests
    {
        private static readonly ChunkCoordinate Chunk =
            new ChunkCoordinate(2, 0, -3);

        [Test]
        public void ConstructorStoresChunk()
        {
            FluidPendingUpdate update =
                CreateUpdate();

            Assert.AreEqual(
                Chunk,
                update.Chunk
            );
        }

        [Test]
        public void ConstructorStoresCoordinates()
        {
            FluidPendingUpdate update =
                CreateUpdate();

            Assert.AreEqual(4, update.X);
            Assert.AreEqual(7, update.Y);
            Assert.AreEqual(12, update.Z);
        }

        [Test]
        public void ConstructorStoresFluidState()
        {
            FluidPendingUpdate update =
                CreateUpdate();

            Assert.AreEqual(
                FluidType.Water,
                update.State.Type
            );

            Assert.AreEqual(
                15,
                update.State.Level
            );
        }

        [Test]
        public void ConstructorStoresDistance()
        {
            FluidPendingUpdate update =
                CreateUpdate(
                    distance: 5
                );

            Assert.AreEqual(
                5,
                update.Distance
            );
        }

        [Test]
        public void DefaultDistanceIsZero()
        {
            FluidPendingUpdate update =
                new FluidPendingUpdate(
                    Chunk,
                    4,
                    7,
                    12,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            Assert.AreEqual(
                0,
                update.Distance
            );
        }

        [Test]
        public void ValidUpdateIsValid()
        {
            FluidPendingUpdate update =
                CreateUpdate();

            Assert.IsTrue(
                update.IsValid
            );
        }

        [Test]
        public void NegativeXIsInvalid()
        {
            FluidPendingUpdate update =
                new FluidPendingUpdate(
                    Chunk,
                    -1,
                    7,
                    12,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            Assert.IsFalse(
                update.IsValid
            );
        }

        [Test]
        public void XOutsideChunkIsInvalid()
        {
            FluidPendingUpdate update =
                new FluidPendingUpdate(
                    Chunk,
                    VoxelConstants.ChunkSize,
                    7,
                    12,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            Assert.IsFalse(
                update.IsValid
            );
        }

        [Test]
        public void NegativeYIsInvalid()
        {
            FluidPendingUpdate update =
                new FluidPendingUpdate(
                    Chunk,
                    4,
                    -1,
                    12,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            Assert.IsFalse(
                update.IsValid
            );
        }

        [Test]
        public void YOutsideChunkIsInvalid()
        {
            FluidPendingUpdate update =
                new FluidPendingUpdate(
                    Chunk,
                    4,
                    VoxelConstants.ChunkSize,
                    12,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            Assert.IsFalse(
                update.IsValid
            );
        }

        [Test]
        public void NegativeZIsInvalid()
        {
            FluidPendingUpdate update =
                new FluidPendingUpdate(
                    Chunk,
                    4,
                    7,
                    -1,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            Assert.IsFalse(
                update.IsValid
            );
        }

        [Test]
        public void ZOutsideChunkIsInvalid()
        {
            FluidPendingUpdate update =
                new FluidPendingUpdate(
                    Chunk,
                    4,
                    7,
                    VoxelConstants.ChunkSize,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            Assert.IsFalse(
                update.IsValid
            );
        }

        [Test]
        public void EmptyFluidStateIsInvalid()
        {
            FluidPendingUpdate update =
                new FluidPendingUpdate(
                    Chunk,
                    4,
                    7,
                    12,
                    FluidState.Empty
                );

            Assert.IsFalse(
                update.IsValid
            );
        }

        [Test]
        public void ToChangePreservesChunk()
        {
            FluidPendingUpdate update =
                CreateUpdate();

            FluidChange change =
                update.ToChange();

            Assert.AreEqual(
                update.Chunk,
                change.TargetChunk
            );
        }

        [Test]
        public void ToChangePreservesCoordinates()
        {
            FluidPendingUpdate update =
                CreateUpdate();

            FluidChange change =
                update.ToChange();

            Assert.AreEqual(
                update.X,
                change.X
            );

            Assert.AreEqual(
                update.Y,
                change.Y
            );

            Assert.AreEqual(
                update.Z,
                change.Z
            );
        }

        [Test]
        public void ToChangePreservesFluidState()
        {
            FluidPendingUpdate update =
                CreateUpdate();

            FluidChange change =
                update.ToChange();

            Assert.AreEqual(
                update.State,
                change.State
            );
        }

        [Test]
        public void EqualUpdatesAreEqual()
        {
            FluidPendingUpdate first =
                CreateUpdate(
                    distance: 4
                );

            FluidPendingUpdate second =
                CreateUpdate(
                    distance: 4
                );

            Assert.AreEqual(
                first,
                second
            );

            Assert.IsTrue(
                first == second
            );

            Assert.IsFalse(
                first != second
            );
        }

        [Test]
        public void DifferentDistanceMakesUpdatesDifferent()
        {
            FluidPendingUpdate first =
                CreateUpdate(
                    distance: 1
                );

            FluidPendingUpdate second =
                CreateUpdate(
                    distance: 2
                );

            Assert.AreNotEqual(
                first,
                second
            );
        }

        [Test]
        public void DifferentCoordinatesMakeUpdatesDifferent()
        {
            FluidPendingUpdate first =
                CreateUpdate();

            FluidPendingUpdate second =
                new FluidPendingUpdate(
                    Chunk,
                    5,
                    7,
                    12,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            Assert.AreNotEqual(
                first,
                second
            );
        }

        [Test]
        public void DifferentFluidStateMakesUpdatesDifferent()
        {
            FluidPendingUpdate first =
                CreateUpdate();

            FluidPendingUpdate second =
                new FluidPendingUpdate(
                    Chunk,
                    4,
                    7,
                    12,
                    new FluidState(
                        FluidType.Water,
                        10
                    )
                );

            Assert.AreNotEqual(
                first,
                second
            );
        }

        [Test]
        public void HashCodeIsEqualForEqualUpdates()
        {
            FluidPendingUpdate first =
                CreateUpdate(
                    distance: 3
                );

            FluidPendingUpdate second =
                CreateUpdate(
                    distance: 3
                );

            Assert.AreEqual(
                first.GetHashCode(),
                second.GetHashCode()
            );
        }

        private static FluidPendingUpdate CreateUpdate(
            int distance = 0)
        {
            return new FluidPendingUpdate(
                Chunk,
                4,
                7,
                12,
                FluidState.Source(
                    FluidType.Water
                ),
                distance
            );
        }
    }
}
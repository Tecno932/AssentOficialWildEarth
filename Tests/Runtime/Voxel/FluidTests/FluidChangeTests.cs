using NUnit.Framework;

namespace WildEarth.Voxel.Tests
{
    public sealed class FluidChangeTests
    {
        [Test]
        public void ChangePreservesTargetChunk()
        {
            ChunkCoordinate targetChunk =
                new ChunkCoordinate(
                    4,
                    2,
                    -3
                );

            FluidState state =
                new FluidState(
                    FluidType.Water,
                    12
                );

            FluidChange change =
                new FluidChange(
                    targetChunk,
                    7,
                    9,
                    11,
                    state
                );

            Assert.AreEqual(
                targetChunk,
                change.TargetChunk
            );

            Assert.AreEqual(
                state,
                change.State
            );
        }

        [Test]
        public void ValidLocalCoordinateIsAccepted()
        {
            FluidChange change =
                new FluidChange(
                    new ChunkCoordinate(
                        0,
                        0,
                        0
                    ),
                    0,
                    0,
                    0,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            Assert.IsTrue(
                change.IsValid
            );
        }

        [Test]
        public void MaximumLocalCoordinateIsAccepted()
        {
            FluidChange change =
                new FluidChange(
                    new ChunkCoordinate(
                        0,
                        0,
                        0
                    ),
                    15,
                    15,
                    15,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            Assert.IsTrue(
                change.IsValid
            );
        }

        [Test]
        public void NegativeLocalCoordinateIsInvalid()
        {
            FluidChange change =
                new FluidChange(
                    new ChunkCoordinate(
                        0,
                        0,
                        0
                    ),
                    -1,
                    8,
                    8,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            Assert.IsFalse(
                change.IsValid
            );
        }

        [Test]
        public void CoordinateAboveChunkIsInvalid()
        {
            FluidChange change =
                new FluidChange(
                    new ChunkCoordinate(
                        0,
                        0,
                        0
                    ),
                    16,
                    8,
                    8,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            Assert.IsFalse(
                change.IsValid
            );
        }

        [Test]
        public void ToWorldCoordinateUsesChunkOrigin()
        {
            ChunkCoordinate targetChunk =
                new ChunkCoordinate(
                    4,
                    2,
                    -3
                );

            FluidChange change =
                new FluidChange(
                    targetChunk,
                    7,
                    9,
                    11,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            WorldVoxelCoordinate world =
                change.ToWorldCoordinate();

            Assert.AreEqual(
                71,
                world.X
            );

            Assert.AreEqual(
                41,
                world.Y
            );

            Assert.AreEqual(
                -37,
                world.Z
            );
        }

        [Test]
        public void NegativeChunkCoordinatesProduceCorrectWorldCoordinate()
        {
            ChunkCoordinate targetChunk =
                new ChunkCoordinate(
                    -2,
                    -1,
                    -3
                );

            FluidChange change =
                new FluidChange(
                    targetChunk,
                    15,
                    14,
                    13,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            WorldVoxelCoordinate world =
                change.ToWorldCoordinate();

            Assert.AreEqual(
                -17,
                world.X
            );

            Assert.AreEqual(
                -2,
                world.Y
            );

            Assert.AreEqual(
                -35,
                world.Z
            );
        }
    }
}
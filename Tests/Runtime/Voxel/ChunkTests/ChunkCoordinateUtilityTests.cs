using NUnit.Framework;

namespace WildEarth.Voxel.Tests
{
    public sealed class ChunkCoordinateUtilityTests
    {
        [Test]
        public void CoordinateInsideChunkStaysInSameChunk()
        {
            ChunkCoordinate source =
                new ChunkCoordinate(5, 2, -3);

            ChunkCoordinate result =
                ChunkCoordinateUtility.ResolveChunk(
                    source,
                    8,
                    7,
                    12,
                    out int x,
                    out int y,
                    out int z
                );

            Assert.AreEqual(
                source,
                result
            );

            Assert.AreEqual(8, x);
            Assert.AreEqual(7, y);
            Assert.AreEqual(12, z);
        }

        [Test]
        public void CoordinateAtPositiveBoundaryMovesToNextChunk()
        {
            ChunkCoordinate source =
                new ChunkCoordinate(0, 0, 0);

            ChunkCoordinate result =
                ChunkCoordinateUtility.ResolveChunk(
                    source,
                    16,
                    8,
                    8,
                    out int x,
                    out int y,
                    out int z
                );

            Assert.AreEqual(
                new ChunkCoordinate(1, 0, 0),
                result
            );

            Assert.AreEqual(0, x);
            Assert.AreEqual(8, y);
            Assert.AreEqual(8, z);
        }

        [Test]
        public void CoordinateBeyondPositiveBoundaryIsNormalized()
        {
            ChunkCoordinate source =
                new ChunkCoordinate(10, 2, -4);

            ChunkCoordinate result =
                ChunkCoordinateUtility.ResolveChunk(
                    source,
                    19,
                    21,
                    35,
                    out int x,
                    out int y,
                    out int z
                );

            Assert.AreEqual(
                new ChunkCoordinate(11, 3, -2),
                result
            );

            Assert.AreEqual(3, x);
            Assert.AreEqual(5, y);
            Assert.AreEqual(3, z);
        }

        [Test]
        public void CoordinateAtNegativeBoundaryMovesToPreviousChunk()
        {
            ChunkCoordinate source =
                new ChunkCoordinate(0, 0, 0);

            ChunkCoordinate result =
                ChunkCoordinateUtility.ResolveChunk(
                    source,
                    -1,
                    8,
                    8,
                    out int x,
                    out int y,
                    out int z
                );

            Assert.AreEqual(
                new ChunkCoordinate(-1, 0, 0),
                result
            );

            Assert.AreEqual(15, x);
            Assert.AreEqual(8, y);
            Assert.AreEqual(8, z);
        }

        [Test]
        public void CoordinateAtNegativeChunkBoundaryIsNormalized()
        {
            ChunkCoordinate source =
                new ChunkCoordinate(0, 0, 0);

            ChunkCoordinate result =
                ChunkCoordinateUtility.ResolveChunk(
                    source,
                    -16,
                    8,
                    8,
                    out int x,
                    out int y,
                    out int z
                );

            Assert.AreEqual(
                new ChunkCoordinate(-1, 0, 0),
                result
            );

            Assert.AreEqual(0, x);
            Assert.AreEqual(8, y);
            Assert.AreEqual(8, z);
        }

        [Test]
        public void CoordinateBeyondNegativeBoundaryIsNormalized()
        {
            ChunkCoordinate source =
                new ChunkCoordinate(10, 2, -4);

            ChunkCoordinate result =
                ChunkCoordinateUtility.ResolveChunk(
                    source,
                    -19,
                    -21,
                    -35,
                    out int x,
                    out int y,
                    out int z
                );

            Assert.AreEqual(
                new ChunkCoordinate(8, 0, -7),
                result
            );

            Assert.AreEqual(13, x);
            Assert.AreEqual(11, y);
            Assert.AreEqual(13, z);
        }

        [Test]
        public void MultipleAxesCanCrossChunkBoundaries()
        {
            ChunkCoordinate source =
                new ChunkCoordinate(3, 4, 5);

            ChunkCoordinate result =
                ChunkCoordinateUtility.ResolveChunk(
                    source,
                    16,
                    -1,
                    32,
                    out int x,
                    out int y,
                    out int z
                );

            Assert.AreEqual(
                new ChunkCoordinate(4, 3, 7),
                result
            );

            Assert.AreEqual(0, x);
            Assert.AreEqual(15, y);
            Assert.AreEqual(0, z);
        }

        [Test]
        public void LargePositiveCoordinatesAreHandled()
        {
            ChunkCoordinate source =
                new ChunkCoordinate(0, 0, 0);

            ChunkCoordinate result =
                ChunkCoordinateUtility.ResolveChunk(
                    source,
                    1000,
                    500,
                    777,
                    out int x,
                    out int y,
                    out int z
                );

            Assert.AreEqual(
                new ChunkCoordinate(62, 31, 48),
                result
            );

            Assert.AreEqual(8, x);
            Assert.AreEqual(4, y);
            Assert.AreEqual(9, z);
        }

        [Test]
        public void LargeNegativeCoordinatesAreHandled()
        {
            ChunkCoordinate source =
                new ChunkCoordinate(0, 0, 0);

            ChunkCoordinate result =
                ChunkCoordinateUtility.ResolveChunk(
                    source,
                    -1000,
                    -500,
                    -777,
                    out int x,
                    out int y,
                    out int z
                );

            Assert.AreEqual(
                new ChunkCoordinate(-63, -32, -49),
                result
            );

            Assert.AreEqual(8, x);
            Assert.AreEqual(12, y);
            Assert.AreEqual(7, z);
        }
    }
}
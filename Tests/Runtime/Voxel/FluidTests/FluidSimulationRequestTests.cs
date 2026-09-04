using NUnit.Framework;

namespace WildEarth.Voxel.Tests
{
    public sealed class FluidSimulationRequestTests
    {
        [Test]
        public void StoresChunkCoordinate()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    10,
                    2,
                    -4
                );

            FluidSimulationRequest request =
                new FluidSimulationRequest(
                    coordinate
                );

            Assert.That(
                request.Chunk,
                Is.EqualTo(coordinate)
            );
        }

        [Test]
        public void RequestsWithSameChunkAreEqual()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    3,
                    1,
                    -2
                );

            FluidSimulationRequest first =
                new FluidSimulationRequest(
                    coordinate
                );

            FluidSimulationRequest second =
                new FluidSimulationRequest(
                    coordinate
                );

            Assert.That(
                first,
                Is.EqualTo(second)
            );

            Assert.That(
                first == second,
                Is.True
            );

            Assert.That(
                first != second,
                Is.False
            );
        }

        [Test]
        public void RequestsWithDifferentChunksAreNotEqual()
        {
            FluidSimulationRequest first =
                new FluidSimulationRequest(
                    new ChunkCoordinate(
                        0,
                        0,
                        0
                    )
                );

            FluidSimulationRequest second =
                new FluidSimulationRequest(
                    new ChunkCoordinate(
                        1,
                        0,
                        0
                    )
                );

            Assert.That(
                first,
                Is.Not.EqualTo(second)
            );

            Assert.That(
                first == second,
                Is.False
            );

            Assert.That(
                first != second,
                Is.True
            );
        }

        [Test]
        public void SameChunkProducesSameHashCode()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    -5,
                    3,
                    8
                );

            FluidSimulationRequest first =
                new FluidSimulationRequest(
                    coordinate
                );

            FluidSimulationRequest second =
                new FluidSimulationRequest(
                    coordinate
                );

            Assert.That(
                first.GetHashCode(),
                Is.EqualTo(
                    second.GetHashCode()
                )
            );
        }

        [Test]
        public void ToStringContainsChunk()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    2,
                    4,
                    6
                );

            FluidSimulationRequest request =
                new FluidSimulationRequest(
                    coordinate
                );

            string text =
                request.ToString();

            Assert.That(
                text,
                Does.Contain("FluidSimulationRequest")
            );

            Assert.That(
                text,
                Does.Contain("Chunk(2, 4, 6)")
            );
        }
    }
}
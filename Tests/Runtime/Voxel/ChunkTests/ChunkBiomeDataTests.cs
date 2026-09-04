using NUnit.Framework;
using Unity.Collections;
using WildEarth.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class ChunkBiomeDataTests
    {
        private ChunkBiomeData data;

        [SetUp]
        public void SetUp()
        {
            data =
                new ChunkBiomeData(
                    Allocator.Persistent
                );
        }

        [TearDown]
        public void TearDown()
        {
            data.Dispose();
        }

        [Test]
        public void Data_IsCreated()
        {
            Assert.That(
                data.IsCreated,
                Is.True
            );
        }

        [Test]
        public void DefaultBiome_IsPlains()
        {
            Assert.That(
                data.Get(0, 0),
                Is.EqualTo(
                    BiomeId.Plains
                )
            );
        }

        [Test]
        public void SetAndGet_ReturnsSameBiome()
        {
            data.Set(
                5,
                7,
                BiomeId.Forest
            );

            Assert.That(
                data.Get(5, 7),
                Is.EqualTo(
                    BiomeId.Forest
                )
            );
        }

        [Test]
        public void OutOfBounds_ReturnsPlains()
        {
            Assert.That(
                data.Get(-1, 0),
                Is.EqualTo(
                    BiomeId.Plains
                )
            );

            Assert.That(
                data.Get(
                    VoxelConstants.ChunkSize,
                    0
                ),
                Is.EqualTo(
                    BiomeId.Plains
                )
            );
        }
    }
}
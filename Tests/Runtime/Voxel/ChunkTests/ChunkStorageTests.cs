using NUnit.Framework;
using WildEarth.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class ChunkStorageTests
    {
        private ChunkDataPool pool;

        private ChunkBiomeDataPool biomePool;

        private ChunkStorage storage;

        [SetUp]
        public void SetUp()
        {
            pool =
                new ChunkDataPool(
                    Unity.Collections.Allocator.Persistent,
                    initialCapacity: 2,
                    maxCapacity: 8
                );

            biomePool =
                new ChunkBiomeDataPool(
                    Unity.Collections.Allocator.Persistent,
                    initialCapacity: 2,
                    maxCapacity: 8
                );

            storage =
                new ChunkStorage(
                    pool,
                    biomePool,
                    initialCapacity: 16
                );
        }

        [TearDown]
        public void TearDown()
        {
            storage.Dispose();
        }

        [Test]
        public void CreateChunk_AddsChunkToStorage()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    10,
                    0,
                    -4
                );

            Chunk chunk =
                storage.Create(
                    coordinate
                );

            Assert.That(
                chunk,
                Is.Not.Null
            );

            Assert.That(
                storage.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                storage.Contains(
                    coordinate
                ),
                Is.True
            );
        }

        [Test]
        public void CreateChunk_StartsInLoadingState()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            Chunk chunk =
                storage.Create(
                    coordinate
                );

            Assert.That(
                chunk.State,
                Is.EqualTo(
                    ChunkState.Loading
                )
            );
        }

        [Test]
        public void CreateChunk_HasVoxelAndBiomeData()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            Chunk chunk =
                storage.Create(
                    coordinate
                );

            Assert.That(
                chunk.Data.IsCreated,
                Is.True
            );

            Assert.That(
                chunk.BiomeData.IsCreated,
                Is.True
            );
        }

        [Test]
        public void RemoveChunk_ReturnsBuffersToPools()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            storage.Create(
                coordinate
            );

            Assert.That(
                pool.AvailableCount,
                Is.EqualTo(1)
            );

            Assert.That(
                biomePool.AvailableCount,
                Is.EqualTo(1)
            );

            storage.Remove(
                coordinate
            );

            Assert.That(
                storage.Count,
                Is.EqualTo(0)
            );

            Assert.That(
                pool.AvailableCount,
                Is.EqualTo(2)
            );

            Assert.That(
                biomePool.AvailableCount,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void ExistingChunk_IsReturnedInsteadOfDuplicated()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    3,
                    0,
                    7
                );

            Chunk first =
                storage.Create(
                    coordinate
                );

            Assert.Throws<System.InvalidOperationException>(
                () =>
                    storage.Create(
                        coordinate
                    )
            );

            Assert.That(
                first.Coordinate,
                Is.EqualTo(
                    coordinate
                )
            );
        }
    }
}
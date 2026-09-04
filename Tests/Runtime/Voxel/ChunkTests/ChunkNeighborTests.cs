using NUnit.Framework;
using Unity.Collections;
using WildEarth.Voxel;
using VoxelData = WildEarth.Voxel.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class ChunkNeighborTests
    {
        private ChunkDataPool dataPool;
        private ChunkBiomeDataPool biomeDataPool;
        private ChunkStorage storage;

        [SetUp]
        public void SetUp()
        {
            dataPool =
                new ChunkDataPool(
                    Allocator.Persistent,
                    16,
                    32
                );

            biomeDataPool =
                new ChunkBiomeDataPool(
                    Allocator.Persistent,
                    16,
                    32
                );

            storage =
                new ChunkStorage(
                    dataPool,
                    biomeDataPool
                );
        }

        [TearDown]
        public void TearDown()
        {
            if (storage != null)
            {
                storage.Dispose();
                storage = null;
                dataPool = null;
                biomeDataPool = null;
            }
        }

        [Test]
        public void North_ReturnsCorrectCoordinate()
        {
            ChunkCoordinate origin =
                new ChunkCoordinate(
                    10,
                    5,
                    -4
                );

            ChunkCoordinate result =
                ChunkNeighborResolver.GetNeighborCoordinate(
                    origin,
                    ChunkNeighborDirection.North
                );

            Assert.That(
                result,
                Is.EqualTo(
                    new ChunkCoordinate(
                        10,
                        5,
                        -3
                    )
                )
            );
        }

        [Test]
        public void South_ReturnsCorrectCoordinate()
        {
            ChunkCoordinate origin =
                new ChunkCoordinate(
                    10,
                    5,
                    -4
                );

            ChunkCoordinate result =
                ChunkNeighborResolver.GetNeighborCoordinate(
                    origin,
                    ChunkNeighborDirection.South
                );

            Assert.That(
                result,
                Is.EqualTo(
                    new ChunkCoordinate(
                        10,
                        5,
                        -5
                    )
                )
            );
        }

        [Test]
        public void East_ReturnsCorrectCoordinate()
        {
            ChunkCoordinate origin =
                new ChunkCoordinate(
                    10,
                    5,
                    -4
                );

            ChunkCoordinate result =
                ChunkNeighborResolver.GetNeighborCoordinate(
                    origin,
                    ChunkNeighborDirection.East
                );

            Assert.That(
                result,
                Is.EqualTo(
                    new ChunkCoordinate(
                        11,
                        5,
                        -4
                    )
                )
            );
        }

        [Test]
        public void West_ReturnsCorrectCoordinate()
        {
            ChunkCoordinate origin =
                new ChunkCoordinate(
                    10,
                    5,
                    -4
                );

            ChunkCoordinate result =
                ChunkNeighborResolver.GetNeighborCoordinate(
                    origin,
                    ChunkNeighborDirection.West
                );

            Assert.That(
                result,
                Is.EqualTo(
                    new ChunkCoordinate(
                        9,
                        5,
                        -4
                    )
                )
            );
        }

        [Test]
        public void Above_ReturnsCorrectCoordinate()
        {
            ChunkCoordinate origin =
                new ChunkCoordinate(
                    10,
                    5,
                    -4
                );

            ChunkCoordinate result =
                ChunkNeighborResolver.GetNeighborCoordinate(
                    origin,
                    ChunkNeighborDirection.Above
                );

            Assert.That(
                result,
                Is.EqualTo(
                    new ChunkCoordinate(
                        10,
                        6,
                        -4
                    )
                )
            );
        }

        [Test]
        public void Below_ReturnsCorrectCoordinate()
        {
            ChunkCoordinate origin =
                new ChunkCoordinate(
                    10,
                    5,
                    -4
                );

            ChunkCoordinate result =
                ChunkNeighborResolver.GetNeighborCoordinate(
                    origin,
                    ChunkNeighborDirection.Below
                );

            Assert.That(
                result,
                Is.EqualTo(
                    new ChunkCoordinate(
                        10,
                        4,
                        -4
                    )
                )
            );
        }

        [Test]
        public void TryGetNeighbor_ReturnsLoadedNeighbor()
        {
            ChunkCoordinate origin =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            ChunkCoordinate north =
                ChunkNeighborResolver.GetNeighborCoordinate(
                    origin,
                    ChunkNeighborDirection.North
                );

            Chunk originChunk =
                storage.Create(origin);

            Chunk northChunk =
                storage.Create(north);

            bool found =
                ChunkNeighborResolver.TryGetNeighbor(
                    storage,
                    origin,
                    ChunkNeighborDirection.North,
                    out Chunk result
                );

            Assert.That(
                found,
                Is.True
            );

            Assert.That(
                result,
                Is.SameAs(northChunk)
            );

            Assert.That(
                result,
                Is.Not.SameAs(originChunk)
            );
        }

        [Test]
        public void TryGetNeighbor_ReturnsFalseWhenNotLoaded()
        {
            ChunkCoordinate origin =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            storage.Create(origin);

            bool found =
                ChunkNeighborResolver.TryGetNeighbor(
                    storage,
                    origin,
                    ChunkNeighborDirection.North,
                    out Chunk neighbor
                );

            Assert.That(
                found,
                Is.False
            );

            Assert.That(
                neighbor,
                Is.Null
            );
        }

        [Test]
        public void TryGetVoxel_ReadsCurrentChunk()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            Chunk chunk =
                storage.Create(coordinate);

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                1,
                2,
                3,
                new VoxelData(42)
            );

            bool resolved =
                ChunkNeighborAccess.TryGetVoxel(
                    storage,
                    coordinate,
                    1,
                    2,
                    3,
                    out VoxelData voxel
                );

            Assert.That(
                resolved,
                Is.True
            );

            Assert.That(
                voxel.BlockId,
                Is.EqualTo(42)
            );
        }

        [Test]
        public void TryGetVoxel_ReadsWestNeighbor()
        {
            ChunkCoordinate origin =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            ChunkCoordinate west =
                new ChunkCoordinate(
                    -1,
                    0,
                    0
                );

            storage.Create(origin);

            Chunk westChunk =
                storage.Create(west);

            ChunkDataAccess.SetVoxel(
                westChunk.Data,
                VoxelConstants.ChunkSize - 1,
                7,
                8,
                new VoxelData(55)
            );

            bool resolved =
                ChunkNeighborAccess.TryGetVoxel(
                    storage,
                    origin,
                    -1,
                    7,
                    8,
                    out VoxelData voxel
                );

            Assert.That(
                resolved,
                Is.True
            );

            Assert.That(
                voxel.BlockId,
                Is.EqualTo(55)
            );
        }

        [Test]
        public void TryGetVoxel_ReadsEastNeighbor()
        {
            ChunkCoordinate origin =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            ChunkCoordinate east =
                new ChunkCoordinate(
                    1,
                    0,
                    0
                );

            storage.Create(origin);

            Chunk eastChunk =
                storage.Create(east);

            ChunkDataAccess.SetVoxel(
                eastChunk.Data,
                0,
                7,
                8,
                new VoxelData(56)
            );

            bool resolved =
                ChunkNeighborAccess.TryGetVoxel(
                    storage,
                    origin,
                    VoxelConstants.ChunkSize,
                    7,
                    8,
                    out VoxelData voxel
                );

            Assert.That(
                resolved,
                Is.True
            );

            Assert.That(
                voxel.BlockId,
                Is.EqualTo(56)
            );
        }

        [Test]
        public void TryGetVoxel_ReadsSouthNeighbor()
        {
            ChunkCoordinate origin =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            ChunkCoordinate south =
                new ChunkCoordinate(
                    0,
                    0,
                    -1
                );

            storage.Create(origin);

            Chunk southChunk =
                storage.Create(south);

            ChunkDataAccess.SetVoxel(
                southChunk.Data,
                7,
                8,
                VoxelConstants.ChunkSize - 1,
                new VoxelData(57)
            );

            bool resolved =
                ChunkNeighborAccess.TryGetVoxel(
                    storage,
                    origin,
                    7,
                    8,
                    -1,
                    out VoxelData voxel
                );

            Assert.That(
                resolved,
                Is.True
            );

            Assert.That(
                voxel.BlockId,
                Is.EqualTo(57)
            );
        }

        [Test]
        public void TryGetVoxel_ReadsNorthNeighbor()
        {
            ChunkCoordinate origin =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            ChunkCoordinate north =
                new ChunkCoordinate(
                    0,
                    0,
                    1
                );

            storage.Create(origin);

            Chunk northChunk =
                storage.Create(north);

            ChunkDataAccess.SetVoxel(
                northChunk.Data,
                7,
                8,
                0,
                new VoxelData(58)
            );

            bool resolved =
                ChunkNeighborAccess.TryGetVoxel(
                    storage,
                    origin,
                    7,
                    8,
                    VoxelConstants.ChunkSize,
                    out VoxelData voxel
                );

            Assert.That(
                resolved,
                Is.True
            );

            Assert.That(
                voxel.BlockId,
                Is.EqualTo(58)
            );
        }

        [Test]
        public void TryGetVoxel_ReadsAboveNeighbor()
        {
            ChunkCoordinate origin =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            ChunkCoordinate above =
                new ChunkCoordinate(
                    0,
                    1,
                    0
                );

            storage.Create(origin);

            Chunk aboveChunk =
                storage.Create(above);

            ChunkDataAccess.SetVoxel(
                aboveChunk.Data,
                7,
                0,
                8,
                new VoxelData(59)
            );

            bool resolved =
                ChunkNeighborAccess.TryGetVoxel(
                    storage,
                    origin,
                    7,
                    VoxelConstants.ChunkSize,
                    8,
                    out VoxelData voxel
                );

            Assert.That(
                resolved,
                Is.True
            );

            Assert.That(
                voxel.BlockId,
                Is.EqualTo(59)
            );
        }

        [Test]
        public void TryGetVoxel_ReadsBelowNeighbor()
        {
            ChunkCoordinate origin =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            ChunkCoordinate below =
                new ChunkCoordinate(
                    0,
                    -1,
                    0
                );

            storage.Create(origin);

            Chunk belowChunk =
                storage.Create(below);

            ChunkDataAccess.SetVoxel(
                belowChunk.Data,
                7,
                VoxelConstants.ChunkSize - 1,
                8,
                new VoxelData(60)
            );

            bool resolved =
                ChunkNeighborAccess.TryGetVoxel(
                    storage,
                    origin,
                    7,
                    -1,
                    8,
                    out VoxelData voxel
                );

            Assert.That(
                resolved,
                Is.True
            );

            Assert.That(
                voxel.BlockId,
                Is.EqualTo(60)
            );
        }

        [Test]
        public void TryGetVoxel_ReturnsFalseWhenNeighborIsNotLoaded()
        {
            ChunkCoordinate origin =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            storage.Create(origin);

            bool resolved =
                ChunkNeighborAccess.TryGetVoxel(
                    storage,
                    origin,
                    -1,
                    5,
                    5,
                    out VoxelData voxel
                );

            Assert.That(
                resolved,
                Is.False
            );

            Assert.That(
                voxel.BlockId,
                Is.EqualTo(BlockIds.Air)
            );
        }

        [Test]
        public void IsBoundary_DetectsAllSixFaces()
        {
            int max =
                VoxelConstants.ChunkSize - 1;

            Assert.That(
                ChunkNeighborAccess.IsBoundary(
                    5,
                    5,
                    max,
                    ChunkNeighborDirection.North
                ),
                Is.True
            );

            Assert.That(
                ChunkNeighborAccess.IsBoundary(
                    5,
                    5,
                    0,
                    ChunkNeighborDirection.South
                ),
                Is.True
            );

            Assert.That(
                ChunkNeighborAccess.IsBoundary(
                    max,
                    5,
                    5,
                    ChunkNeighborDirection.East
                ),
                Is.True
            );

            Assert.That(
                ChunkNeighborAccess.IsBoundary(
                    0,
                    5,
                    5,
                    ChunkNeighborDirection.West
                ),
                Is.True
            );

            Assert.That(
                ChunkNeighborAccess.IsBoundary(
                    5,
                    max,
                    5,
                    ChunkNeighborDirection.Above
                ),
                Is.True
            );

            Assert.That(
                ChunkNeighborAccess.IsBoundary(
                    5,
                    0,
                    5,
                    ChunkNeighborDirection.Below
                ),
                Is.True
            );
        }
    }
}
using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using WildEarth.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class VoxelMeshBuilderTests
    {
        private BlockRegistry registry;
        private BlockRuntimeDatabase blockDatabase;
        private ChunkDataPool dataPool;
        private ChunkBiomeDataPool biomePool;
        private ChunkStorage storage;
        private VoxelMeshBuilder builder;

        [SetUp]
        public void SetUp()
        {
            registry =
                AssetDatabase.LoadAssetAtPath<BlockRegistry>(
                    "Assets/_Project/Data/Blocks/BlockRegistry.asset"
                );

            Assert.That(
                registry,
                Is.Not.Null,
                "No se encontró BlockRegistry.asset."
            );

            blockDatabase =
                new BlockRuntimeDatabase(
                    registry,
                    Allocator.Persistent
                );

            dataPool =
                new ChunkDataPool(
                    Allocator.Persistent,
                    2,
                    4
                );

            biomePool =
                new ChunkBiomeDataPool(
                    Allocator.Persistent,
                    2,
                    4
                );

            storage =
                new ChunkStorage(
                    dataPool,
                    biomePool
                );

            builder =
                new VoxelMeshBuilder(
                    blockDatabase
                );
        }

        [TearDown]
        public void TearDown()
        {
            storage?.Dispose();
            storage = null;

            blockDatabase?.Dispose();
            blockDatabase = null;

            registry = null;
            dataPool = null;
            biomePool = null;
            builder = null;
        }

        [Test]
        public void EmptyChunk_ProducesEmptyMesh()
        {
            Chunk chunk =
                storage.Create(
                    new ChunkCoordinate(0, 0, 0)
                );

            ChunkMeshData mesh =
                builder.Build(
                    chunk,
                    storage
                );

            Assert.That(
                mesh.VertexCount,
                Is.EqualTo(0)
            );

            Assert.That(
                mesh.TriangleIndexCount,
                Is.EqualTo(0)
            );

            Assert.That(
                mesh.FaceCount,
                Is.EqualTo(0)
            );

            mesh.Dispose();
        }

        [Test]
        public void IsolatedCube_ProducesSixFaces()
        {
            Chunk chunk =
                storage.Create(
                    new ChunkCoordinate(0, 0, 0)
                );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                0,
                0,
                0,
                new WildEarth.Voxel.Voxel(1)
            );

            ChunkMeshData mesh =
                builder.Build(
                    chunk,
                    storage
                );

            Assert.That(
                mesh.FaceCount,
                Is.EqualTo(6)
            );

            Assert.That(
                mesh.VertexCount,
                Is.EqualTo(24)
            );

            Assert.That(
                mesh.TriangleIndexCount,
                Is.EqualTo(36)
            );

            mesh.Dispose();
        }

        [Test]
        public void TwoAdjacentCubes_HideSharedFace()
        {
            Chunk chunk =
                storage.Create(
                    new ChunkCoordinate(0, 0, 0)
                );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                0,
                0,
                0,
                new WildEarth.Voxel.Voxel(1)
            );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                1,
                0,
                0,
                new WildEarth.Voxel.Voxel(1)
            );

            ChunkMeshData mesh =
                builder.Build(
                    chunk,
                    storage
                );

            Assert.That(
                mesh.FaceCount,
                Is.EqualTo(10)
            );

            Assert.That(
                mesh.VertexCount,
                Is.EqualTo(40)
            );

            Assert.That(
                mesh.TriangleIndexCount,
                Is.EqualTo(60)
            );

            mesh.Dispose();
        }

        [Test]
        public void UnloadedNeighbor_KeepsBoundaryFaceVisible()
        {
            Chunk chunk =
                storage.Create(
                    new ChunkCoordinate(0, 0, 0)
                );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                VoxelConstants.ChunkSize - 1,
                0,
                0,
                new WildEarth.Voxel.Voxel(1)
            );

            ChunkMeshData mesh =
                builder.Build(
                    chunk,
                    storage
                );

            Assert.That(
                mesh.FaceCount,
                Is.EqualTo(6)
            );

            mesh.Dispose();
        }

        [Test]
        public void LoadedNeighbor_HidesBoundaryFace()
        {
            Chunk chunk =
                storage.Create(
                    new ChunkCoordinate(0, 0, 0)
                );

            Chunk neighbor =
                storage.Create(
                    new ChunkCoordinate(1, 0, 0)
                );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                VoxelConstants.ChunkSize - 1,
                0,
                0,
                new WildEarth.Voxel.Voxel(1)
            );

            ChunkDataAccess.SetVoxel(
                neighbor.Data,
                0,
                0,
                0,
                new WildEarth.Voxel.Voxel(1)
            );

            ChunkMeshData mesh =
                builder.Build(
                    chunk,
                    storage
                );

            Assert.That(
                mesh.FaceCount,
                Is.EqualTo(5)
            );

            Assert.That(
                mesh.VertexCount,
                Is.EqualTo(20)
            );

            Assert.That(
                mesh.TriangleIndexCount,
                Is.EqualTo(30)
            );

            mesh.Dispose();
        }
    }
}
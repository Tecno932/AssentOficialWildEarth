using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using WildEarth.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class ChunkMeshRendererTests
    {
        private BlockRegistry registry;
        private BlockRuntimeDatabase blockDatabase;
        private ChunkDataPool dataPool;
        private ChunkBiomeDataPool biomePool;
        private ChunkStorage storage;
        private VoxelMeshBuilder builder;
        private VoxelAtlasSettings atlasSettings;
        private GameObject rendererObject;
        private ChunkMeshRenderer renderer;

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

                atlasSettings =
                    ScriptableObject.CreateInstance<VoxelAtlasSettings>();

                Assert.That(
                    atlasSettings,
                    Is.Not.Null,
                    "No se pudo crear VoxelAtlasSettings."
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
                    blockDatabase,
                    atlasSettings
                );

            rendererObject =
                new GameObject(
                    "ChunkMeshRendererTest"
                );

            renderer =
                rendererObject.AddComponent<ChunkMeshRenderer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (rendererObject != null)
            {
                Object.DestroyImmediate(
                    rendererObject
                );

                rendererObject = null;
                renderer = null;
            }

            storage?.Dispose();
            storage = null;

            blockDatabase?.Dispose();
            blockDatabase = null;

            registry = null;
            dataPool = null;
            biomePool = null;
            builder = null;

            if (atlasSettings != null)
            {
                Object.DestroyImmediate(atlasSettings);
                atlasSettings = null;
            }
        }

        [Test]
        public void Apply_BuilderMesh_CreatesUnityMesh()
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

            ChunkMeshData meshData =
                builder.Build(
                    chunk,
                    storage
                );

            renderer.Apply(
                meshData,
                null
            );

            Assert.That(
                renderer.Mesh,
                Is.Not.Null
            );

            Assert.That(
                renderer.Mesh.vertexCount,
                Is.EqualTo(24)
            );

            Assert.That(
                renderer.Mesh.triangles.Length,
                Is.EqualTo(36)
            );

            Assert.That(
                renderer.Mesh.uv.Length,
                Is.EqualTo(24)
            );

            Assert.That(
                renderer.Mesh.bounds.size,
                Is.EqualTo(
                    new Vector3(1f, 1f, 1f)
                )
            );

            meshData.Dispose();
        }
    }
}
using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using WildEarth.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class VoxelWorldRendererTests
    {
        private BlockRegistry blockRegistry;
        private BiomeRegistryAsset biomeRegistryAsset;
        private OreRegistryAsset oreRegistryAsset;
        private FluidRegistryAsset fluidRegistryAsset;

        private BlockRuntimeDatabase blockDatabase;
        private ChunkDataPool dataPool;
        private ChunkBiomeDataPool biomePool;
        private ChunkStorage storage;
        private VoxelMeshBuilder builder;
        private VoxelAtlasSettings atlasSettings;

        private VoxelWorld world;

        private GameObject rendererObject;
        private VoxelWorldRenderer renderer;

        private Material testMaterial;

        private Texture2D testAtlas;

        [SetUp]
        public void SetUp()
        {
            blockRegistry =
                AssetDatabase.LoadAssetAtPath<BlockRegistry>(
                    "Assets/_Project/Data/Blocks/BlockRegistry.asset"
                );

            biomeRegistryAsset =
                AssetDatabase.LoadAssetAtPath<BiomeRegistryAsset>(
                    "Assets/_Project/Data/Biomes/BiomeRegistry.asset"
                );

            oreRegistryAsset =
                AssetDatabase.LoadAssetAtPath<OreRegistryAsset>(
                    "Assets/_Project/Data/Ores/OreRegistry.asset"
                );

            fluidRegistryAsset =
                AssetDatabase.LoadAssetAtPath<FluidRegistryAsset>(
                    "Assets/_Project/Data/Fluids/FluidRegistry.asset"
                );

            atlasSettings =
                ScriptableObject.CreateInstance<VoxelAtlasSettings>();

            Assert.That(
                blockRegistry,
                Is.Not.Null
            );

            Assert.That(
                biomeRegistryAsset,
                Is.Not.Null
            );

            Assert.That(
                oreRegistryAsset,
                Is.Not.Null
            );

            Assert.That(
                fluidRegistryAsset,
                Is.Not.Null
            );

            blockDatabase =
                new BlockRuntimeDatabase(
                    blockRegistry,
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

            world =
                new VoxelWorld(
                    VoxelWorldSettings.Default,
                    ChunkGenerationSettings.Default,
                    biomeRegistryAsset,
                    blockRegistry,
                    oreRegistryAsset,
                    fluidRegistryAsset
                );

            world.Initialize();

            rendererObject =
                new GameObject(
                    "VoxelWorldRendererTest"
                );

            renderer =
                rendererObject.AddComponent<VoxelWorldRenderer>();

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit"
                );

            Assert.That(
                shader,
                Is.Not.Null,
                "No se encontró el shader Universal Render Pipeline/Lit."
            );

            testAtlas =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/_Project/Art/Textures/atlas.png"
                );

            Assert.That(
                testAtlas,
                Is.Not.Null,
                "No se encontró el atlas en Assets/_Project/Art/Textures/atlas.png."
            );

            testMaterial =
                new Material(shader)
                {
                    name = "VoxelWorldRendererTestMaterial"
                };

            if (testMaterial.HasProperty("_BaseMap"))
            {
                testMaterial.SetTexture(
                    "_BaseMap",
                    testAtlas
                );
            }

            if (testMaterial.HasProperty("_MainTex"))
            {
                testMaterial.SetTexture(
                    "_MainTex",
                    testAtlas
                );
            }

            SerializedObject serializedRenderer =
                new SerializedObject(renderer);

            SerializedProperty materialProperty =
                serializedRenderer.FindProperty(
                    "defaultMaterial"
                );

            Assert.That(
                materialProperty,
                Is.Not.Null,
                "No se encontró la propiedad defaultMaterial."
            );

            materialProperty.objectReferenceValue =
                testMaterial;

            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();

            renderer.Initialize(
                world,
                builder
            );
        }

        [TearDown]
        public void TearDown()
        {
            testAtlas = null;

            if (testMaterial != null)
            {
                Object.DestroyImmediate(
                    testMaterial
                );

                testMaterial = null;
            }

            if (rendererObject != null)
            {
                Object.DestroyImmediate(
                    rendererObject
                );

                rendererObject = null;
                renderer = null;
            }

            if (atlasSettings != null)
            {
                Object.DestroyImmediate(atlasSettings);
                atlasSettings = null;
            }

            world?.Dispose();
            world = null;

            storage?.Dispose();
            storage = null;

            blockDatabase?.Dispose();
            blockDatabase = null;

            blockRegistry = null;
            biomeRegistryAsset = null;
            oreRegistryAsset = null;
            fluidRegistryAsset = null;

            dataPool = null;
            biomePool = null;
            builder = null;
        }

        [Test]
        public void Initialize_CreatesUsableRenderer()
        {
            Assert.That(
                renderer,
                Is.Not.Null
            );

            Assert.That(
                renderer.RenderedChunkCount,
                Is.EqualTo(0)
            );

            Assert.That(
                world.IsInitialized,
                Is.True
            );
        }

        [Test]
        public void RenderChunk_CreatesRendererAndMesh()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    1,
                    2,
                    3
                );

            Chunk chunk =
                world.LoadChunk(
                    coordinate
                );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                0,
                0,
                0,
                new WildEarth.Voxel.Voxel(
                    1
                )
            );

            chunk.MarkGenerated();
            chunk.MarkNeedsMesh();

            renderer.RenderChunk(
                chunk
            );

            Assert.That(
                renderer.RenderedChunkCount,
                Is.EqualTo(1)
            );

            Assert.That(
                chunk.State,
                Is.EqualTo(ChunkState.Ready)
            );

            Assert.That(
                chunk.NeedsMesh,
                Is.False
            );

            Transform chunkTransform =
                rendererObject.transform
                    .GetChild(0);

            Assert.That(
                chunkTransform.localPosition,
                Is.EqualTo(
                    new Vector3(
                        16f,
                        32f,
                        48f
                    )
                )
            );

            ChunkMeshRenderer chunkRenderer =
                chunkTransform.GetComponent<ChunkMeshRenderer>();

            Assert.That(
                chunkRenderer,
                Is.Not.Null
            );

            Assert.That(
                chunkRenderer.Mesh,
                Is.Not.Null
            );

            Assert.That(
                chunkRenderer.Mesh.vertexCount,
                Is.EqualTo(24)
            );

            Assert.That(
                chunkRenderer.Mesh.triangles.Length,
                Is.EqualTo(36)
            );
        }

        [Test]
        public void RemoveChunk_RemovesRenderer()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    2,
                    0,
                    -1
                );

            Chunk chunk =
                world.LoadChunk(
                    coordinate
                );

            chunk.MarkGenerated();
            chunk.MarkNeedsMesh();

            renderer.RenderChunk(
                chunk
            );

            Assert.That(
                renderer.RenderedChunkCount,
                Is.EqualTo(1)
            );

            renderer.RemoveChunk(
                coordinate
            );

            Assert.That(
                renderer.RenderedChunkCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void Clear_RemovesAllRenderers()
        {
            ChunkCoordinate first =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            ChunkCoordinate second =
                new ChunkCoordinate(
                    1,
                    0,
                    0
                );

            Chunk firstChunk =
                world.LoadChunk(
                    first
                );

            Chunk secondChunk =
                world.LoadChunk(
                    second
                );

            firstChunk.MarkGenerated();
            firstChunk.MarkNeedsMesh();

            secondChunk.MarkGenerated();
            secondChunk.MarkNeedsMesh();

            renderer.RenderChunk(
                firstChunk
            );

            renderer.RenderChunk(
                secondChunk
            );

            Assert.That(
                renderer.RenderedChunkCount,
                Is.EqualTo(2)
            );

            renderer.Clear();

            Assert.That(
                renderer.RenderedChunkCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void RenderCompletedChunks_RendersGeneratedChunks()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    1,
                    0,
                    0
                );

            Chunk chunk =
                world.LoadChunk(
                    coordinate
                );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                0,
                0,
                0,
                new WildEarth.Voxel.Voxel(
                    1
                )
            );

            chunk.MarkGenerated();
            chunk.MarkNeedsMesh();

            renderer.RenderCompletedChunks();

            Assert.That(
                renderer.RenderedChunkCount,
                Is.EqualTo(0)
            );

            world.Generator.Schedule(
                chunk
            );

            world.CompleteGeneration();

            renderer.RenderCompletedChunks();

            Assert.That(
                renderer.RenderedChunkCount,
                Is.EqualTo(1)
            );

            Assert.That(
                chunk.State,
                Is.EqualTo(ChunkState.Ready)
            );

            Assert.That(
                chunk.NeedsMesh,
                Is.False
            );
        }
    }
}
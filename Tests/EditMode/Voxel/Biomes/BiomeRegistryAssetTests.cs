using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace WildEarth.Voxel.Tests
{
    public sealed class BiomeRegistryAssetTests
    {
        private BiomeRegistryAsset asset;

        private BiomeDefinition plains;
        private BiomeDefinition forest;

        [SetUp]
        public void SetUp()
        {
            plains =
                ScriptableObject.CreateInstance<
                    BiomeDefinition
                >();

            forest =
                ScriptableObject.CreateInstance<
                    BiomeDefinition
                >();

            ConfigureBiome(
                plains,
                BiomeId.Plains
            );

            ConfigureBiome(
                forest,
                BiomeId.Forest
            );

            asset =
                ScriptableObject.CreateInstance<
                    BiomeRegistryAsset
                >();

            SerializedObject serialized =
                new SerializedObject(
                    asset
                );

            SerializedProperty definitions =
                serialized.FindProperty(
                    "definitions"
                );

            definitions.arraySize = 2;

            definitions
                .GetArrayElementAtIndex(0)
                .objectReferenceValue =
                plains;

            definitions
                .GetArrayElementAtIndex(1)
                .objectReferenceValue =
                forest;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            if (asset != null)
                Object.DestroyImmediate(asset);

            if (plains != null)
                Object.DestroyImmediate(plains);

            if (forest != null)
                Object.DestroyImmediate(forest);
        }

        [Test]
        public void Asset_ContainsDefinitions()
        {
            Assert.That(
                asset.Definitions,
                Is.Not.Null
            );

            Assert.That(
                asset.Definitions.Count,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void Registry_CanBeCreatedFromAsset()
        {
            BiomeRegistry registry =
                new BiomeRegistry(asset);

            Assert.That(
                registry.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                registry.GetRuntimeData(
                    BiomeId.Plains
                ).Id,
                Is.EqualTo(BiomeId.Plains)
            );

            Assert.That(
                registry.GetRuntimeData(
                    BiomeId.Forest
                ).Id,
                Is.EqualTo(BiomeId.Forest)
            );
        }

        private static void ConfigureBiome(
            BiomeDefinition definition,
            BiomeId id)
        {
            SerializedObject serialized =
                new SerializedObject(
                    definition
                );

            serialized.FindProperty("id")
                .enumValueIndex =
                (int)id;

            serialized.FindProperty(
                "temperatureMin"
            ).floatValue = 0f;

            serialized.FindProperty(
                "temperatureMax"
            ).floatValue = 1f;

            serialized.FindProperty(
                "moistureMin"
            ).floatValue = 0f;

            serialized.FindProperty(
                "moistureMax"
            ).floatValue = 1f;

            serialized.FindProperty(
                "terrainHeightMultiplier"
            ).floatValue = 1f;

            serialized.FindProperty(
                "terrainHeightOffset"
            ).floatValue = 0f;

            serialized.FindProperty(
                "surfaceBlockId"
            ).intValue = 3;

            serialized.FindProperty(
                "subSurfaceBlockId"
            ).intValue = 2;

            serialized.FindProperty(
                "deepBlockId"
            ).intValue = 1;

            serialized.FindProperty(
                "subSurfaceDepth"
            ).intValue = 3;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
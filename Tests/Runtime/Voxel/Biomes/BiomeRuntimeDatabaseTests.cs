using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace WildEarth.Voxel.Tests
{
    public sealed class BiomeRuntimeDatabaseTests
    {
        private BiomeDefinition plains;
        private BiomeDefinition forest;

        [SetUp]
        public void SetUp()
        {
            plains =
                CreateBiome(
                    BiomeId.Plains,
                    0.35f,
                    0.75f,
                    0.25f,
                    0.70f
                );

            forest =
                CreateBiome(
                    BiomeId.Forest,
                    0.35f,
                    0.80f,
                    0.60f,
                    1.00f
                );
        }

        [TearDown]
        public void TearDown()
        {
            if (plains != null)
                Object.DestroyImmediate(plains);

            if (forest != null)
                Object.DestroyImmediate(forest);
        }

        [Test]
        public void Registry_CreatesRuntimeData()
        {
            BiomeRegistry registry =
                new BiomeRegistry(
                    new[]
                    {
                        plains,
                        forest
                    }
                );

            BiomeRuntimeData data =
                registry.GetRuntimeData(
                    BiomeId.Plains
                );

            Assert.That(
                data.Id,
                Is.EqualTo(BiomeId.Plains)
            );

            Assert.That(
                data.TemperatureMin,
                Is.EqualTo(0.35f)
            );

            Assert.That(
                data.MoistureMax,
                Is.EqualTo(0.70f)
            );
        }

        [Test]
        public void RuntimeDatabase_CreatesNativeData()
        {
            BiomeRegistry registry =
                new BiomeRegistry(
                    new[]
                    {
                        plains,
                        forest
                    }
                );

            using BiomeRuntimeDatabase database =
                new BiomeRuntimeDatabase(
                    registry,
                    Allocator.TempJob
                );

            Assert.That(
                database.IsCreated,
                Is.True
            );

            Assert.That(
                database.Length,
                Is.GreaterThanOrEqualTo(2)
            );
        }

        [Test]
        public void Selector_ReturnsPlainsForPlainsClimate()
        {
            BiomeRegistry registry =
                new BiomeRegistry(
                    new[]
                    {
                        plains,
                        forest
                    }
                );

            using BiomeRuntimeDatabase database =
                new BiomeRuntimeDatabase(
                    registry,
                    Allocator.TempJob
                );

            BiomeId result =
                BiomeSelector.Select(
                    database.AsNativeArray(),
                    temperature: 0.50f,
                    moisture: 0.40f
                );

            Assert.That(
                result,
                Is.EqualTo(BiomeId.Plains)
            );
        }

        [Test]
        public void Selector_ReturnsForestForWetClimate()
        {
            BiomeRegistry registry =
                new BiomeRegistry(
                    new[]
                    {
                        plains,
                        forest
                    }
                );

            using BiomeRuntimeDatabase database =
                new BiomeRuntimeDatabase(
                    registry,
                    Allocator.TempJob
                );

            BiomeId result =
                BiomeSelector.Select(
                    database.AsNativeArray(),
                    temperature: 0.55f,
                    moisture: 0.85f
                );

            Assert.That(
                result,
                Is.EqualTo(BiomeId.Forest)
            );
        }

        private static BiomeDefinition CreateBiome(
            BiomeId id,
            float temperatureMin,
            float temperatureMax,
            float moistureMin,
            float moistureMax)
        {
            BiomeDefinition definition =
                ScriptableObject.CreateInstance<
                    BiomeDefinition
                >();

            SerializedObject serialized =
                new SerializedObject(
                    definition
                );

            serialized.FindProperty("id").enumValueIndex =
                (int)id;

            serialized.FindProperty("temperatureMin").floatValue =
                temperatureMin;

            serialized.FindProperty("temperatureMax").floatValue =
                temperatureMax;

            serialized.FindProperty("moistureMin").floatValue =
                moistureMin;

            serialized.FindProperty("moistureMax").floatValue =
                moistureMax;

            serialized.FindProperty("terrainHeightMultiplier").floatValue =
                1f;

            serialized.FindProperty("terrainHeightOffset").floatValue =
                0f;

            serialized.FindProperty("surfaceBlockId").intValue =
                3;

            serialized.FindProperty("subSurfaceBlockId").intValue =
                2;

            serialized.FindProperty("deepBlockId").intValue =
                1;

            serialized.FindProperty("subSurfaceDepth").intValue =
                3;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            return definition;
        }
    }
}
using NUnit.Framework;
using Unity.Collections;

namespace WildEarth.Voxel.Tests
{
    public sealed class BiomeSelectorTests
    {
        private NativeArray<BiomeRuntimeData> biomes;

        [SetUp]
        public void SetUp()
        {
            biomes =
                new NativeArray<BiomeRuntimeData>(
                    4,
                    Allocator.TempJob
                );

            biomes[0] =
                CreateBiome(
                    BiomeId.Plains,
                    0.35f,
                    0.75f,
                    0.25f,
                    0.70f
                );

            biomes[1] =
                CreateBiome(
                    BiomeId.Forest,
                    0.35f,
                    0.80f,
                    0.60f,
                    1.00f
                );

            biomes[2] =
                CreateBiome(
                    BiomeId.Desert,
                    0.70f,
                    1.00f,
                    0.00f,
                    0.35f
                );

            biomes[3] =
                CreateBiome(
                    BiomeId.Tundra,
                    0.00f,
                    0.30f,
                    0.00f,
                    1.00f
                );
        }

        [TearDown]
        public void TearDown()
        {
            if (biomes.IsCreated)
                biomes.Dispose();
        }

        [Test]
        public void Selector_PlainsClimate_ReturnsPlains()
        {
            BiomeId result =
                BiomeSelector.Select(
                    biomes,
                    0.50f,
                    0.40f
                );

            Assert.That(
                result,
                Is.EqualTo(BiomeId.Plains)
            );
        }

        [Test]
        public void Selector_WetClimate_ReturnsForest()
        {
            BiomeId result =
                BiomeSelector.Select(
                    biomes,
                    0.55f,
                    0.85f
                );

            Assert.That(
                result,
                Is.EqualTo(BiomeId.Forest)
            );
        }

        [Test]
        public void Selector_HotDryClimate_ReturnsDesert()
        {
            BiomeId result =
                BiomeSelector.Select(
                    biomes,
                    0.90f,
                    0.15f
                );

            Assert.That(
                result,
                Is.EqualTo(BiomeId.Desert)
            );
        }

        [Test]
        public void Selector_ColdClimate_ReturnsTundra()
        {
            BiomeId result =
                BiomeSelector.Select(
                    biomes,
                    0.15f,
                    0.50f
                );

            Assert.That(
                result,
                Is.EqualTo(BiomeId.Tundra)
            );
        }

        private static BiomeRuntimeData CreateBiome(
            BiomeId id,
            float temperatureMin,
            float temperatureMax,
            float moistureMin,
            float moistureMax)
        {
            return new BiomeRuntimeData
            {
                Id = id,

                TemperatureMin =
                    temperatureMin,

                TemperatureMax =
                    temperatureMax,

                MoistureMin =
                    moistureMin,

                MoistureMax =
                    moistureMax,

                TerrainHeightMultiplier = 1f,
                TerrainHeightOffset = 0f,

                SurfaceBlockId = 3,
                SubSurfaceBlockId = 2,
                DeepBlockId = 1,

                SubSurfaceDepth = 3
            };
        }
    }
}
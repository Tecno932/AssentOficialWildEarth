using NUnit.Framework;
using Unity.Mathematics;
using WildEarth.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class TerrainNoiseTests
    {
        [Test]
        public void Sample_IsDeterministic()
        {
            float2 position =
                new float2(
                    1234.5f,
                    -987.25f
                );

            float first =
                TerrainNoise.Sample(
                    position,
                    0.005f,
                    12345
                );

            float second =
                TerrainNoise.Sample(
                    position,
                    0.005f,
                    12345
                );

            Assert.That(
                first,
                Is.EqualTo(second)
            );
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentNoise()
        {
            float2 position =
                new float2(
                    1234.5f,
                    -987.25f
                );

            float first =
                TerrainNoise.Sample(
                    position,
                    0.005f,
                    12345
                );

            float second =
                TerrainNoise.Sample(
                    position,
                    0.005f,
                    54321
                );

            Assert.That(
                first,
                Is.Not.EqualTo(second)
            );
        }

        [Test]
        public void Sample01_IsInsideExpectedRange()
        {
            float2 position =
                new float2(
                    100f,
                    200f
                );

            float value =
                TerrainNoise.Sample01(
                    position,
                    0.005f,
                    12345
                );

            Assert.That(
                value,
                Is.GreaterThanOrEqualTo(0f)
            );

            Assert.That(
                value,
                Is.LessThanOrEqualTo(1f)
            );
        }

        [Test]
        public void Fractal_IsDeterministic()
        {
            float2 position =
                new float2(
                    500f,
                    -300f
                );

            float first =
                TerrainNoise.Fractal(
                    position,
                    0.0025f,
                    18f,
                    4,
                    2f,
                    0.5f,
                    12345
                );

            float second =
                TerrainNoise.Fractal(
                    position,
                    0.0025f,
                    18f,
                    4,
                    2f,
                    0.5f,
                    12345
                );

            Assert.That(
                first,
                Is.EqualTo(second)
            );
        }
    }
}
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    public static class OreNoise
    {
        public static float Sample(
            float3 position,
            float frequency,
            int seed)
        {
            float3 seedOffset =
                CreateSeedOffset(seed);

            return SampleWithSeedOffset(
                position,
                frequency,
                seedOffset);
        }

        public static float Sample01(
            float3 position,
            float frequency,
            int seed)
        {
            float value =
                Sample(
                    position,
                    frequency,
                    seed);

            return (value + 1f) * 0.5f;
        }

        public static float Fractal01(
            float3 position,
            float frequency,
            int octaves,
            float lacunarity,
            float persistence,
            int seed)
        {
            if (octaves <= 0)
                return 0.5f;

            float total = 0f;
            float amplitude = 1f;
            float normalization = 0f;

            float currentFrequency =
                frequency;

            for (int octave = 0;
                 octave < octaves;
                 octave++)
            {
                float value =
                    Sample01(
                        position,
                        currentFrequency,
                        seed + octave * 1297);

                total +=
                    value *
                    amplitude;

                normalization +=
                    amplitude;

                currentFrequency *=
                    lacunarity;

                amplitude *=
                    persistence;
            }

            if (normalization <= 0f)
                return 0.5f;

            return total / normalization;
        }

        public static float Fractal01Cached(
            float3 position,
            float frequency,
            int octaves,
            float lacunarity,
            float persistence,
            float3 seedOffset0,
            float3 seedOffset1,
            float3 seedOffset2)
        {
            if (octaves <= 0)
                return 0.5f;

            if (octaves == 1)
            {
                float value =
                    SampleWithSeedOffset(
                        position,
                        frequency,
                        seedOffset0);

                return
                    (value + 1f) * 0.5f;
            }

            float amplitude0 = 1f;
            float amplitude1 =
                persistence;

            float value0 =
                SampleWithSeedOffset(
                    position,
                    frequency,
                    seedOffset0);

            float value1 =
                SampleWithSeedOffset(
                    position,
                    frequency * lacunarity,
                    seedOffset1);

            value0 =
                (value0 + 1f) * 0.5f;

            value1 =
                (value1 + 1f) * 0.5f;

            if (octaves == 2)
            {
                float total =
                    value0 * amplitude0 +
                    value1 * amplitude1;

                float normalization =
                    amplitude0 +
                    amplitude1;

                return
                    total /
                    normalization;
            }

            float amplitude2 =
                amplitude1 *
                persistence;

            float value2 =
                SampleWithSeedOffset(
                    position,
                    frequency *
                    lacunarity *
                    lacunarity,
                    seedOffset2);

            value2 =
                (value2 + 1f) * 0.5f;

            if (octaves == 3)
            {
                float total =
                    value0 * amplitude0 +
                    value1 * amplitude1 +
                    value2 * amplitude2;

                float normalization =
                    amplitude0 +
                    amplitude1 +
                    amplitude2;

                return
                    total /
                    normalization;
            }

            return Fractal01(
                position,
                frequency,
                octaves,
                lacunarity,
                persistence,
                0);
        }

        public static float3 CreateSeedOffset(
            int seed)
        {
            float x =
                math.sin(
                    seed * 12.9898f) *
                43758.5453f;

            float y =
                math.sin(
                    seed * 78.233f) *
                43758.5453f;

            float z =
                math.sin(
                    seed * 37.719f) *
                43758.5453f;

            return new float3(
                math.frac(x) * 10000f,
                math.frac(y) * 10000f,
                math.frac(z) * 10000f);
        }

        private static float SampleWithSeedOffset(
            float3 position,
            float frequency,
            float3 seedOffset)
        {
            float3 samplePosition =
                position *
                frequency;

            return noise.snoise(
                samplePosition +
                seedOffset);
        }
    }
}
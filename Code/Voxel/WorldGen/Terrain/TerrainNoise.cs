using Unity.Mathematics;

namespace WildEarth.Voxel
{
    public static class TerrainNoise
    {
        public static float Sample(
            float2 position,
            float frequency,
            int seed)
        {
            float2 seedOffset =
                CreateSeedOffset(seed);

            return SampleWithSeedOffset(
                position,
                frequency,
                seedOffset);
        }

        public static float Sample01(
            float2 position,
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

        public static float Fractal(
            float2 position,
            float frequency,
            float amplitude,
            int octaves,
            float lacunarity,
            float persistence,
            int seed)
        {
            if (octaves <= 0 ||
                amplitude <= 0f)
            {
                return 0f;
            }

            float total = 0f;
            float normalization = 0f;

            float currentFrequency =
                frequency;

            float currentAmplitude =
                amplitude;

            for (int octave = 0;
                 octave < octaves;
                 octave++)
            {
                float value =
                    Sample(
                        position,
                        currentFrequency,
                        seed + octave * 1013);

                total +=
                    value *
                    currentAmplitude;

                normalization +=
                    currentAmplitude;

                currentFrequency *=
                    lacunarity;

                currentAmplitude *=
                    persistence;
            }

            if (normalization <= 0f)
                return 0f;

            return total / normalization * amplitude;
        }

        public static float Fractal01(
            float2 position,
            float frequency,
            int octaves,
            float lacunarity,
            float persistence,
            int seed)
        {
            float value =
                Fractal(
                    position,
                    frequency,
                    1f,
                    octaves,
                    lacunarity,
                    persistence,
                    seed);

            return math.clamp(
                (value + 1f) * 0.5f,
                0f,
                1f);
        }

        public static float Fractal01Cached(
            float2 position,
            float frequency,
            int octaves,
            float lacunarity,
            float persistence,
            float2 seedOffset0,
            float2 seedOffset1,
            float2 seedOffset2,
            float2 seedOffset3)
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
                float2 seedOffset;

                switch (octave)
                {
                    case 0:
                        seedOffset = seedOffset0;
                        break;

                    case 1:
                        seedOffset = seedOffset1;
                        break;

                    case 2:
                        seedOffset = seedOffset2;
                        break;

                    case 3:
                        seedOffset = seedOffset3;
                        break;

                    default:
                        return Fractal01(
                            position,
                            frequency,
                            octaves,
                            lacunarity,
                            persistence,
                            0);
                }

                float value =
                    SampleWithSeedOffset(
                        position,
                        currentFrequency,
                        seedOffset);

                value =
                    (value + 1f) * 0.5f;

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

        public static float2 CreateSeedOffset(
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

            return new float2(
                math.frac(x) * 10000f,
                math.frac(y) * 10000f);
        }

        private static float SampleWithSeedOffset(
            float2 position,
            float frequency,
            float2 seedOffset)
        {
            float2 samplePosition =
                position * frequency;

            return noise.snoise(
                samplePosition + seedOffset);
        }
    }
}
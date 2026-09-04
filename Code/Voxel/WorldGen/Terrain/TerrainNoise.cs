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

            float2 samplePosition =
                position * frequency;

            return noise.snoise(
                samplePosition + seedOffset
            );
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
                    seed
                );

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
                        seed + octave * 1013
                    );

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
                    seed
                );

            return math.clamp(
                (value + 1f) * 0.5f,
                0f,
                1f
            );
        }

        private static float2 CreateSeedOffset(
            int seed)
        {
            float x =
                math.sin(
                    seed * 12.9898f
                ) *
                43758.5453f;

            float y =
                math.sin(
                    seed * 78.233f
                ) *
                43758.5453f;

            return new float2(
                math.frac(x) * 10000f,
                math.frac(y) * 10000f
            );
        }
    }
}
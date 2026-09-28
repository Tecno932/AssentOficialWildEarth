using Unity.Mathematics;

namespace WildEarth.Voxel
{
    public static class CaveNoise
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
                        seed + octave * 977);

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

            float total = 0f;
            float amplitude = 1f;
            float normalization = 0f;

            float currentFrequency =
                frequency;

            for (int octave = 0;
                 octave < octaves;
                 octave++)
            {
                float3 seedOffset;

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
                position * frequency;

            return noise.snoise(
                samplePosition +
                seedOffset);
        }
    }
}
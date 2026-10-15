
using System;

namespace WildEarth.Voxel
{
    public enum ToolQuality : byte
    {
        Poor = 0,
        Common = 1,
        Fine = 2,
        Excellent = 3,
        Masterwork = 4
    }

    public static class ToolQualityUtility
    {
        public static float GetModifier(
            ToolQuality quality)
        {
            switch (quality)
            {
                case ToolQuality.Poor:
                    return 0.75f;

                case ToolQuality.Common:
                    return 1f;

                case ToolQuality.Fine:
                    return 1.05f;

                case ToolQuality.Excellent:
                    return 1.25f;

                case ToolQuality.Masterwork:
                    return 1.50f;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(quality),
                        quality,
                        null
                    );
            }
        }

        public static MaterialStats Apply(
            MaterialStats stats,
            ToolQuality quality)
        {
            float modifier = GetModifier(quality);

            return new MaterialStats(
                stats.Density,
                stats.Hardness * modifier,
                stats.Resistance * modifier,
                stats.Toughness * modifier,
                stats.MeltingTemperature,
                stats.WorkingTemperature,
                stats.EdgeRetention * modifier,
                stats.EdgeAptitude * modifier
            );
        }
    }
}
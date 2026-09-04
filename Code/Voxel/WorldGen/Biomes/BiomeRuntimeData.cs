using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct BiomeRuntimeData
    {
        public BiomeId Id;

        public float TemperatureMin;
        public float TemperatureMax;

        public float MoistureMin;
        public float MoistureMax;

        public float TerrainHeightMultiplier;
        public float TerrainHeightOffset;

        public ushort SurfaceBlockId;
        public ushort SubSurfaceBlockId;
        public ushort DeepBlockId;

        public byte SubSurfaceDepth;

        public bool MatchesClimate(
            float temperature,
            float moisture)
        {
            return temperature >= TemperatureMin &&
                   temperature <= TemperatureMax &&
                   moisture >= MoistureMin &&
                   moisture <= MoistureMax;
        }
    }
}
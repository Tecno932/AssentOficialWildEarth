using UnityEngine;

namespace WildEarth.Voxel
{
    [CreateAssetMenu(
        fileName = "BiomeDefinition",
        menuName = "WildEarth/Voxel/Biome Definition"
    )]
    public sealed class BiomeDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private BiomeId id;
        [SerializeField] private string biomeName;

        [Header("Climate")]
        [SerializeField, Range(0f, 1f)]
        private float temperatureMin;

        [SerializeField, Range(0f, 1f)]
        private float temperatureMax = 1f;

        [SerializeField, Range(0f, 1f)]
        private float moistureMin;

        [SerializeField, Range(0f, 1f)]
        private float moistureMax = 1f;

        [Header("Terrain")]
        [SerializeField]
        private float terrainHeightMultiplier = 1f;

        [SerializeField]
        private float terrainHeightOffset;

        [Header("Blocks")]
        [SerializeField]
        private ushort surfaceBlockId;

        [SerializeField]
        private ushort subSurfaceBlockId;

        [SerializeField]
        private ushort deepBlockId;

        [SerializeField, Min(0)]
        private byte subSurfaceDepth = 3;

        public BiomeId Id => id;

        public string BiomeName =>
            biomeName;

        public float TemperatureMin =>
            temperatureMin;

        public float TemperatureMax =>
            temperatureMax;

        public float MoistureMin =>
            moistureMin;

        public float MoistureMax =>
            moistureMax;

        public float TerrainHeightMultiplier =>
            terrainHeightMultiplier;

        public float TerrainHeightOffset =>
            terrainHeightOffset;

        public ushort SurfaceBlockId =>
            surfaceBlockId;

        public ushort SubSurfaceBlockId =>
            subSurfaceBlockId;

        public ushort DeepBlockId =>
            deepBlockId;

        public byte SubSurfaceDepth =>
            subSurfaceDepth;

        public BiomeRuntimeData ToRuntimeData()
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

                TerrainHeightMultiplier =
                    terrainHeightMultiplier,

                TerrainHeightOffset =
                    terrainHeightOffset,

                SurfaceBlockId =
                    surfaceBlockId,

                SubSurfaceBlockId =
                    subSurfaceBlockId,

                DeepBlockId =
                    deepBlockId,

                SubSurfaceDepth =
                    subSurfaceDepth
            };
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(biomeName))
                biomeName = name;

            if (temperatureMax < temperatureMin)
                temperatureMax = temperatureMin;

            if (moistureMax < moistureMin)
                moistureMax = moistureMin;

            if (terrainHeightMultiplier < 0f)
                terrainHeightMultiplier = 0f;
        }

#endif
    }
}
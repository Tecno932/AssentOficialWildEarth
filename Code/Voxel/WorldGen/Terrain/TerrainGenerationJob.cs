using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace WildEarth.Voxel
{
    [BurstCompile]
    public struct TerrainGenerationJob : IJob
    {
        public ChunkGenerationContext Context;
        public TerrainGenerationSettings Settings;
        public BiomeGenerationSettings BiomeSettings;

        public NativeArray<Voxel> Voxels;

        [ReadOnly]
        public NativeArray<BiomeId> Biomes;

        [ReadOnly]
        public NativeArray<BiomeRuntimeData> BiomeDatabase;

        public NativeArray<int> SurfaceHeights;

        public void Execute()
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            float2 continentalSeedOffset =
                TerrainNoise.CreateSeedOffset(
                    Context.Seed + 1000);

            float2 erosionSeedOffset0 =
                TerrainNoise.CreateSeedOffset(
                    Context.Seed + 2000);

            float2 erosionSeedOffset1 =
                TerrainNoise.CreateSeedOffset(
                    Context.Seed + 2000 + 1013);

            float2 erosionSeedOffset2 =
                TerrainNoise.CreateSeedOffset(
                    Context.Seed + 2000 + 2026);

            float2 peaksSeedOffset0 =
                TerrainNoise.CreateSeedOffset(
                    Context.Seed + 3000);

            float2 peaksSeedOffset1 =
                TerrainNoise.CreateSeedOffset(
                    Context.Seed + 3000 + 1013);

            float2 peaksSeedOffset2 =
                TerrainNoise.CreateSeedOffset(
                    Context.Seed + 3000 + 2026);

            float2 peaksSeedOffset3 =
                TerrainNoise.CreateSeedOffset(
                    Context.Seed + 3000 + 3039);

            float2 detailSeedOffset =
                TerrainNoise.CreateSeedOffset(
                    Context.Seed + 4000);

            for (int z = 0;
                 z < chunkSize;
                 z++)
            {
                for (int x = 0;
                     x < chunkSize;
                     x++)
                {
                    int columnIndex =
                        x + z * chunkSize;

                    BiomeId biomeId =
                        Biomes[columnIndex];

                    BiomeRuntimeData biome =
                        GetBiomeData(biomeId);

                    int worldX =
                        Context.WorldOrigin.x + x;

                    int worldZ =
                        Context.WorldOrigin.z + z;

                    float temperature =
                        CalculateTemperature(
                            worldX,
                            worldZ
                        );

                    float moisture =
                        CalculateMoisture(
                            worldX,
                            worldZ
                        );

                    GetTerrainBiomeBlend(
                        temperature,
                        moisture,
                        biome,
                        out BiomeRuntimeData secondaryBiome,
                        out float secondaryWeight
                    );

                    int terrainHeight =
                        CalculateTerrainHeight(
                            worldX,
                            worldZ,
                            biome,
                            secondaryBiome,
                            secondaryWeight,
                            continentalSeedOffset,
                            erosionSeedOffset0,
                            erosionSeedOffset1,
                            erosionSeedOffset2,
                            peaksSeedOffset0,
                            peaksSeedOffset1,
                            peaksSeedOffset2,
                            peaksSeedOffset3,
                            detailSeedOffset
                        );

                    SurfaceHeights[columnIndex] =
                        terrainHeight;

                    for (int y = 0;
                         y < chunkSize;
                         y++)
                    {
                        int worldY =
                            Context.WorldOrigin.y + y;

                        ushort blockId =
                            ResolveBlock(
                                worldY,
                                terrainHeight,
                                biome
                            );

                        int index =
                            VoxelIndex.ToIndex(
                                x,
                                y,
                                z
                            );

                        Voxels[index] =
                            new Voxel(blockId);
                    }
                }
            }
        }

        private float CalculateTemperature(
            int worldX,
            int worldZ)
        {
            float2 seedOffset =
                new float2(
                    Context.Seed * 0.371f,
                    Context.Seed * 0.619f
                );

            float2 position =
                new float2(
                    worldX,
                    worldZ
                );

            position *=
                BiomeSettings.TemperatureFrequency;

            float value =
                noise.snoise(
                    position +
                    seedOffset
                );

            return
                (value + 1f) * 0.5f;
        }

        private float CalculateMoisture(
            int worldX,
            int worldZ)
        {
            int moistureSeed =
                Context.Seed +
                BiomeSettings.BiomeSeedOffset;

            float2 seedOffset =
                new float2(
                    moistureSeed * 0.271f,
                    moistureSeed * 0.733f
                );

            float2 position =
                new float2(
                    worldX,
                    worldZ
                );

            position *=
                BiomeSettings.MoistureFrequency;

            float value =
                noise.snoise(
                    position +
                    seedOffset
                );

            return
                (value + 1f) * 0.5f;
        }

private void GetTerrainBiomeBlend(
    float temperature,
    float moisture,
    BiomeRuntimeData primaryBiome,
    out BiomeRuntimeData secondaryBiome,
    out float secondaryWeight)
{
    secondaryBiome =
        primaryBiome;

    secondaryWeight =
        0f;

    float primaryDistance =
        CalculateClimateDistance(
            primaryBiome,
            temperature,
            moisture
        );

    float bestSecondaryDistance =
        float.MaxValue;

    for (int i = 0;
         i < BiomeDatabase.Length;
         i++)
    {
        BiomeRuntimeData candidate =
            BiomeDatabase[i];

        if (candidate.Id ==
            primaryBiome.Id)
        {
            continue;
        }

        float distance =
            CalculateClimateDistance(
                candidate,
                temperature,
                moisture
            );

        if (distance >=
            bestSecondaryDistance)
        {
            continue;
        }

        bestSecondaryDistance =
            distance;

        secondaryBiome =
            candidate;
    }

    if (bestSecondaryDistance ==
        float.MaxValue)
    {
        return;
    }

    float distanceDifference =
        math.abs(
            bestSecondaryDistance -
            primaryDistance
        );

    const float blendRange =
        0.20f;

    float normalized =
        math.saturate(
            1f -
            distanceDifference /
            blendRange
        );

    secondaryWeight =
        normalized *
        normalized *
        (3f -
         2f * normalized);
}

        private float CalculateBiomeScore(
            BiomeRuntimeData biome,
            float temperature,
            float moisture)
        {
            float temperatureCenter =
                (
                    biome.TemperatureMin +
                    biome.TemperatureMax
                ) * 0.5f;

            float moistureCenter =
                (
                    biome.MoistureMin +
                    biome.MoistureMax
                ) * 0.5f;

            float temperatureDistance =
                temperature -
                temperatureCenter;

            float moistureDistance =
                moisture -
                moistureCenter;

            return
                temperatureDistance *
                temperatureDistance +
                moistureDistance *
                moistureDistance;
        }

private float CalculateClimateDistance(
    BiomeRuntimeData biome,
    float temperature,
    float moisture)
{
    float temperatureDistance = 0f;

    if (temperature <
        biome.TemperatureMin)
    {
        temperatureDistance =
            biome.TemperatureMin -
            temperature;
    }
    else if (temperature >
             biome.TemperatureMax)
    {
        temperatureDistance =
            temperature -
            biome.TemperatureMax;
    }

    float moistureDistance = 0f;

    if (moisture <
        biome.MoistureMin)
    {
        moistureDistance =
            biome.MoistureMin -
            moisture;
    }
    else if (moisture >
             biome.MoistureMax)
    {
        moistureDistance =
            moisture -
            biome.MoistureMax;
    }

    return math.sqrt(
        temperatureDistance *
        temperatureDistance +
        moistureDistance *
        moistureDistance
    );
}

        private BiomeRuntimeData GetBiomeData(
            BiomeId biomeId)
        {
            int index =
                (int)biomeId;

            if (index < 0 ||
                index >= BiomeDatabase.Length)
            {
                return CreateFallbackBiome();
            }

            return BiomeDatabase[index];
        }

        private BiomeRuntimeData CreateFallbackBiome()
        {
            return new BiomeRuntimeData
            {
                Id = BiomeId.Plains,

                TemperatureMin = 0f,
                TemperatureMax = 1f,

                MoistureMin = 0f,
                MoistureMax = 1f,

                TerrainHeightMultiplier = 1f,
                TerrainHeightOffset = 0f,

                SurfaceBlockId =
                    Settings.GrassBlockId,

                SubSurfaceBlockId =
                    Settings.DirtBlockId,

                DeepBlockId =
                    Settings.StoneBlockId,

                SubSurfaceDepth = 3
            };
        }

private int CalculateTerrainHeight(
    int worldX,
    int worldZ,
    BiomeRuntimeData biome,
    BiomeRuntimeData secondaryBiome,
    float secondaryWeight,
    float2 continentalSeedOffset,
    float2 erosionSeedOffset0,
    float2 erosionSeedOffset1,
    float2 erosionSeedOffset2,
    float2 peaksSeedOffset0,
    float2 peaksSeedOffset1,
    float2 peaksSeedOffset2,
    float2 peaksSeedOffset3,
    float2 detailSeedOffset)
{
    float2 position =
        new float2(
            worldX,
            worldZ
        );

    float continentalness =
        TerrainNoise.Sample01(
            position,
            Settings.ContinentalFrequency,
            Context.Seed + 1000
        );

    float continentalShape =
        continentalness * 2f - 1f;

    float height =
        Settings.BaseHeight;

    height +=
        continentalShape *
        Settings.ContinentalAmplitude;

    float erosion =
        TerrainNoise.Fractal01Cached(
            position,
            Settings.ErosionFrequency,
            3,
            2f,
            0.5f,
            erosionSeedOffset0,
            erosionSeedOffset1,
            erosionSeedOffset2,
            erosionSeedOffset2
        );

    float erosionShape =
        erosion * 2f - 1f;

    height +=
        erosionShape *
        Settings.ErosionAmplitude;

    float peaks =
        TerrainNoise.Fractal01Cached(
            position,
            Settings.PeaksFrequency,
            4,
            2f,
            0.5f,
            peaksSeedOffset0,
            peaksSeedOffset1,
            peaksSeedOffset2,
            peaksSeedOffset3
        );

    float peaksShape =
        math.pow(
            peaks,
            1.75f
        );

    height +=
        peaksShape *
        Settings.PeaksAmplitude;

    float detail =
        TerrainNoise.Sample(
            position,
            Settings.DetailFrequency,
            Context.Seed + 4000
        );

    height +=
        detail *
        Settings.DetailAmplitude;

    /*
     * La altura ya no depende del bioma seleccionado.
     *
     * Calculamos una influencia continua de todos los biomas
     * usando distancia a sus centros climáticos.
     *
     * Esto evita saltos cuando BiomeSelector cambia de un bioma
     * a otro en una columna vecina.
     */
    float continuousMultiplier =
        CalculateContinuousTerrainMultiplier(
            worldX,
            worldZ
        );

    float terrainOffset =
        height -
        Settings.BaseHeight;

    /*
     * El multiplicador afecta de forma continua al relieve.
     * No se aplica ningún offset discreto del bioma.
     */
    height =
        Settings.BaseHeight +
        terrainOffset *
        continuousMultiplier;

    height =
        math.clamp(
            height,
            VoxelConstants.MinVoxelY,
            VoxelConstants.MaxVoxelY
        );

    return
        (int)math.round(height);
}

private float CalculateContinuousTerrainMultiplier(
    int worldX,
    int worldZ)
{
    float temperature =
        CalculateTemperature(
            worldX,
            worldZ
        );

    float moisture =
        CalculateMoisture(
            worldX,
            worldZ
        );

    float weightedMultiplier = 0f;
    float totalWeight = 0f;

    /* Paredes de las montañas */
    const float climateRadius = 0.28f;

    for (int i = 0;
         i < BiomeDatabase.Length;
         i++)
    {
        BiomeRuntimeData biome =
            BiomeDatabase[i];

        float temperatureCenter =
            (
                biome.TemperatureMin +
                biome.TemperatureMax
            ) * 0.5f;

        float moistureCenter =
            (
                biome.MoistureMin +
                biome.MoistureMax
            ) * 0.5f;

        float temperatureDistance =
            temperature -
            temperatureCenter;

        float moistureDistance =
            moisture -
            moistureCenter;

        float distanceSquared =
            temperatureDistance *
            temperatureDistance +
            moistureDistance *
            moistureDistance;

        float weight =
            math.exp(
                -distanceSquared /
                (
                    climateRadius *
                    climateRadius
                )
            );

        weightedMultiplier +=
            biome.TerrainHeightMultiplier *
            weight;

        totalWeight +=
            weight;
    }

    if (totalWeight <= 0.0001f)
        return 1f;

    float multiplier =
        weightedMultiplier /
        totalWeight;

    return
        math.max(
            0.5f,
            multiplier
        );
}

        private ushort ResolveBlock(
            int worldY,
            int terrainHeight,
            BiomeRuntimeData biome)
        {
            if (worldY > terrainHeight)
                return BlockIds.Air;

            if (worldY == terrainHeight)
                return biome.SurfaceBlockId;

            int depth =
                terrainHeight -
                worldY;

            if (depth <=
                biome.SubSurfaceDepth)
            {
                return biome.SubSurfaceBlockId;
            }

            return biome.DeepBlockId;
        }
    }
}
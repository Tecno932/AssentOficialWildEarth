using NUnit.Framework;
using UnityEditor;
using System;
using UnityEngine;
using WildEarth.Voxel;
using Unity.Collections;
using Unity.Jobs;
using VoxelData = WildEarth.Voxel.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class ChunkGenerationPipelineTests
    {
        private BiomeRegistryAsset biomeRegistryAsset;
        private BlockRegistry blockRegistry;
        private OreRegistryAsset oreRegistryAsset;
        private FluidRegistryAsset fluidRegistryAsset;

        [SetUp]
        public void SetUp()
        {
            biomeRegistryAsset =
                AssetDatabase.LoadAssetAtPath<BiomeRegistryAsset>(
                    "Assets/_Project/Data/Biomes/BiomeRegistry.asset"
                );

            Assert.That(
                biomeRegistryAsset,
                Is.Not.Null,
                "No se encontró BiomeRegistry.asset."
            );

            blockRegistry =
                AssetDatabase.LoadAssetAtPath<BlockRegistry>(
                    "Assets/_Project/Data/Blocks/BlockRegistry.asset"
                );

            Assert.That(
                blockRegistry,
                Is.Not.Null,
                "No se encontró BlockRegistry.asset."
            );

            oreRegistryAsset =
                AssetDatabase.LoadAssetAtPath<OreRegistryAsset>(
                    "Assets/_Project/Data/Ores/OreRegistry.asset"
                );

            Assert.That(
                oreRegistryAsset,
                Is.Not.Null,
                "No se encontró OreRegistry.asset."
            );

            fluidRegistryAsset =
                AssetDatabase.LoadAssetAtPath<FluidRegistryAsset>(
                    "Assets/_Project/Data/Fluids/FluidRegistry.asset"
                );

            Assert.That(
                fluidRegistryAsset,
                Is.Not.Null,
                "No se encontró FluidRegistry.asset."
            );
        }

        [Test]
        public void Pipeline_FluidsDisabled_ProducesTerrainBelowSeaLevel()
        {
            ChunkGenerationSettings settings =
                ChunkGenerationSettings.Default;

            settings.Fluids.Enabled = false;
            settings.Fluids.GenerateWater = false;

            VoxelWorld world =
                CreateWorld(settings);

            try
            {
                world.Initialize();

                GenerateTestChunks(
                    world
                );

                TerrainScanResult result =
                    ScanTerrain(
                        world
                    );

                Assert.That(
                    result.ColumnCount,
                    Is.GreaterThan(0),
                    "No se encontraron columnas con terreno."
                );

                Assert.That(
                    result.MinimumWorldHeight,
                    Is.LessThan(
                        settings.Terrain.SeaLevel
                    ),
                    "Con los fluidos desactivados no se encontró " +
                    "terreno por debajo del SeaLevel."
                );
            }
            finally
            {
                world.Dispose();
            }
        }

        [Test]
        public void Pipeline_FluidsEnabled_PreservesTerrainConfiguration()
        {
            ChunkGenerationSettings settings =
                ChunkGenerationSettings.Default;

            settings.Fluids.Enabled = true;
            settings.Fluids.GenerateWater = true;

            VoxelWorld world =
                CreateWorld(settings);

            try
            {
                world.Initialize();

                GenerateTestChunks(
                    world
                );

                TerrainScanResult result =
                    ScanTerrainIgnoringFluids(
                        world
                    );

                Assert.That(
                    result.ColumnCount,
                    Is.GreaterThan(0),
                    "No se encontraron columnas con terreno."
                );

                Assert.That(
                    result.MinimumWorldHeight,
                    Is.LessThan(
                        settings.Terrain.SeaLevel
                    ),
                    "El pipeline con fluidos activados no conserva " +
                    "ningún terreno por debajo del SeaLevel."
                );
            }
            finally
            {
                world.Dispose();
            }
        }

        [Test]
        public void Pipeline_FluidsEnabled_GeneratesWaterAboveLowTerrain()
        {
            ChunkGenerationSettings settings =
                ChunkGenerationSettings.Default;

            settings.Fluids.Enabled = true;
            settings.Fluids.GenerateWater = true;
            settings.Caves.Enabled = false;
            settings.Ores.Enabled = false;

            using VoxelWorld world =
                CreateWorld(settings);

            world.Initialize();

            Chunk chunk =
                world.LoadAndGenerateChunk(
                    new ChunkCoordinate(-8, 2, -3)
                );

            world.CompleteGeneration();

            int waterVoxelCount =
                CountWaterVoxelsInChunk(chunk);

            Assert.That(
                chunk.State,
                Is.EqualTo(ChunkState.Generated)
            );

            Assert.That(
                waterVoxelCount,
                Is.GreaterThan(0),
                "El pipeline debería generar agua en el chunk conocido " +
                "con terreno bajo el SeaLevel."
            );
        }

        [Test]
        public void Pipeline_FluidsEnabled_WithOresDisabled_GeneratesWaterAboveLowTerrain()
        {
            ChunkGenerationSettings settings =
                ChunkGenerationSettings.Default;

            settings.Fluids.Enabled = true;
            settings.Fluids.GenerateWater = true;
            settings.Ores.Enabled = false;
            settings.Caves.Enabled = false;

            using VoxelWorld world =
                CreateWorld(settings);

            world.Initialize();

            Chunk chunk =
                world.LoadAndGenerateChunk(
                    new ChunkCoordinate(-8, 2, -3)
                );

            world.CompleteGeneration();

            int waterVoxelCount =
                CountWaterVoxelsInChunk(chunk);

            Assert.That(
                chunk.State,
                Is.EqualTo(ChunkState.Generated)
            );

            Assert.That(
                waterVoxelCount,
                Is.GreaterThan(0),
                "El pipeline debería generar agua con los ores desactivados."
            );
        }

        [Test]
        public void Pipeline_FluidsDisabled_ProducesNoWater()
        {
            ChunkGenerationSettings settings =
                ChunkGenerationSettings.Default;

            settings.Fluids.Enabled = false;
            settings.Fluids.GenerateWater = false;

            VoxelWorld world =
                CreateWorld(settings);

            try
            {
                world.Initialize();

                GenerateTestChunks(
                    world
                );

                int waterVoxelCount =
                    CountWaterVoxels(
                        world
                    );

                Assert.That(
                    waterVoxelCount,
                    Is.EqualTo(0),
                    "El pipeline produjo agua aunque la generación " +
                    "de fluidos estaba desactivada."
                );
            }
            finally
            {
                world.Dispose();
            }
        }

        [Test]
        public void FluidRegistry_WaterRuntimeData_HasExpectedBlockId()
        {
            ChunkGenerationSettings settings =
                ChunkGenerationSettings.Default;

            VoxelWorld world =
                CreateWorld(settings);

            try
            {
                world.Initialize();

                var water =
                    world.Fluids.GetRuntimeData(
                        FluidType.Water
                    );

                UnityEngine.Debug.Log(
                    "[Fluid Registry Debug] " +
                    $"Type={water.Type}, " +
                    $"BlockId={water.BlockId}, " +
                    $"MaxLevel={water.MaxLevel}, " +
                    $"HorizontalFlowDecay={water.HorizontalFlowDecay}, " +
                    $"VerticalFlowDecay={water.VerticalFlowDecay}, " +
                    $"IsLava={water.IsLava}"
                );

                Assert.That(
                    water.Type,
                    Is.EqualTo(FluidType.Water),
                    "El registro no devolvió el fluido Water."
                );

                Assert.That(
                    water.IsValid,
                    Is.True,
                    "El runtime data del agua no es válido."
                );

                Assert.That(
                    water.BlockId,
                    Is.EqualTo(7),
                    "El BlockId real del agua no coincide con el BlockId " +
                    "que están utilizando los tests."
                );

                Assert.That(
                    water.MaxLevel,
                    Is.GreaterThan(0),
                    "El agua tiene MaxLevel inválido."
                );
            }
            finally
            {
                world.Dispose();
            }
        }

    [Test]
    public void Pipeline_FluidsEnabled_DebugWaterByChunkY()
    {
        var settings = ChunkGenerationSettings.Default;

        settings.Fluids.Enabled = true;
        settings.Fluids.GenerateWater = true;
        settings.Caves.Enabled = false;
        settings.Ores.Enabled = false;

        using var world = CreateWorld(settings);

        world.Initialize();

        Chunk chunk = world.LoadAndGenerateChunk(
            new ChunkCoordinate(-8, 2, -3)
        );

        Assert.That(chunk, Is.Not.Null);

        world.CompleteGeneration();

        Assert.That(chunk.Data.IsCreated, Is.True);

        int water = CountWaterVoxelsInChunk(chunk);

        Debug.Log(
            $"[Pipeline Fluid Y Debug] " +
            $"Chunk={chunk.Coordinate}, " +
            $"WorldOriginY={chunk.Coordinate.Y * VoxelConstants.ChunkSize}, " +
            $"water={water}");

        Assert.That(water, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void FluidGenerationJob_Directly_GeneratesWater()
    {
        FluidGenerationSettings fluidSettings =
            FluidGenerationSettings.Default;

        TerrainGenerationSettings terrainSettings =
            TerrainGenerationSettings.Default;

        VoxelWorld world =
            CreateWorld(
                ChunkGenerationSettings.Default
            );

        try
        {
            world.Initialize();

            FluidRuntimeData water =
                world.Fluids.GetRuntimeData(
                    FluidType.Water
                );

            NativeArray<VoxelData> voxels =
                new NativeArray<VoxelData>(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.TempJob
                );

            NativeArray<int> surfaceHeights =
                new NativeArray<int>(
                    VoxelConstants.ChunkSize *
                    VoxelConstants.ChunkSize,
                    Allocator.TempJob
                );

            try
            {
                for (int i = 0;
                    i < surfaceHeights.Length;
                    i++)
                {
                    surfaceHeights[i] = 15;
                }

                FluidGenerationJob job =
                    new FluidGenerationJob
                    {
                        Settings =
                            fluidSettings,

                        TerrainSettings =
                            terrainSettings,

                        Voxels =
                            voxels,

                        SurfaceHeights =
                            surfaceHeights,

                        Water =
                            water,

                        WorldOriginY = 16
                    };

                JobHandle handle =
                    job.Schedule();

                handle.Complete();

                int waterCount = 0;
                int firstWaterIndex = -1;

                for (int i = 0;
                    i < voxels.Length;
                    i++)
                {
                    if (voxels[i].BlockId !=
                        water.BlockId)
                    {
                        continue;
                    }

                    waterCount++;

                    if (firstWaterIndex < 0)
                    {
                        firstWaterIndex = i;
                    }
                }

                Debug.Log(
                    "[Direct Fluid Debug] " +
                    $"Enabled={fluidSettings.Enabled}, " +
                    $"GenerateWater={fluidSettings.GenerateWater}, " +
                    $"SeaLevel={terrainSettings.SeaLevel}, " +
                    $"WaterBlockId={water.BlockId}, " +
                    $"WaterMaxLevel={water.MaxLevel}, " +
                    $"WorldOriginY=16, " +
                    $"SurfaceHeight=15, " +
                    $"WaterVoxels={waterCount}, " +
                    $"FirstWaterIndex={firstWaterIndex}"
                );

                Assert.Greater(
                    waterCount,
                    0,
                    "FluidGenerationJob no generó agua " +
                    "cuando se ejecutó directamente."
                );
            }
            finally
            {
                if (voxels.IsCreated)
                {
                    voxels.Dispose();
                }

                if (surfaceHeights.IsCreated)
                {
                    surfaceHeights.Dispose();
                }
            }
        }
        finally
        {
            world.Dispose();
        }
    }

[Test]
public void Pipeline_FluidsEnabled_WithCavesDisabled_GeneratesWater()
{
    ChunkGenerationSettings settings =
        ChunkGenerationSettings.Default;

    settings.Fluids.Enabled = true;
    settings.Fluids.GenerateWater = true;
    settings.Caves.Enabled = false;
    settings.Ores.Enabled = false;

    using VoxelWorld world =
        CreateWorld(settings);

    world.Initialize();

    Chunk chunk =
        world.LoadAndGenerateChunk(
            new ChunkCoordinate(-8, 2, -3)
        );

    world.CompleteGeneration();

    int waterVoxelCount =
        CountWaterVoxelsInChunk(chunk);

    Assert.That(
        chunk.State,
        Is.EqualTo(ChunkState.Generated)
    );

    Assert.That(
        waterVoxelCount,
        Is.GreaterThan(0),
        "El pipeline debería generar agua con las caves desactivadas."
    );
}

[Test]
public void Direct_TerrainThenFluid_GeneratesWater()
{
    ChunkGenerationSettings settings =
        ChunkGenerationSettings.Default;

    settings.Fluids.Enabled = true;
    settings.Fluids.GenerateWater = true;
    settings.Caves.Enabled = false;
    settings.Ores.Enabled = false;

    VoxelWorld world =
        CreateWorld(settings);

    try
    {
        world.Initialize();

        BiomeRuntimeDatabase biomeDatabase =
            new BiomeRuntimeDatabase(
                world.Biomes,
                Allocator.TempJob
            );

        FluidRuntimeDatabase fluidDatabase =
            new FluidRuntimeDatabase(
                world.Fluids,
                Allocator.TempJob
            );

        try
        {
            FluidRuntimeData water =
                fluidDatabase.Get(
                    FluidType.Water
                );

            const int chunkX = -8;
            const int chunkY = 2;
            const int chunkZ = -3;

            ChunkGenerationContext context =
                new ChunkGenerationContext(
                    settings.Seed,
                    new Unity.Mathematics.int3(
                        chunkX,
                        chunkY,
                        chunkZ
                    )
                );

            Debug.Log(
                "[Direct Terrain/Fluid Target] " +
                $"Chunk=({chunkX},{chunkY},{chunkZ}), " +
                $"WorldOrigin=({context.WorldOrigin.x}," +
                $"{context.WorldOrigin.y}," +
                $"{context.WorldOrigin.z}), " +
                $"SeaLevel={settings.Terrain.SeaLevel}, " +
                $"WaterBlockId={water.BlockId}, " +
                $"WaterMaxLevel={water.MaxLevel}"
            );

            NativeArray<VoxelData> voxels =
                new NativeArray<VoxelData>(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.TempJob,
                    NativeArrayOptions.ClearMemory
                );

            NativeArray<BiomeId> biomes =
                new NativeArray<BiomeId>(
                    ChunkBiomeData.Size,
                    Allocator.TempJob,
                    NativeArrayOptions.ClearMemory
                );

            NativeArray<int> surfaceHeights =
                new NativeArray<int>(
                    VoxelConstants.ChunkSize *
                    VoxelConstants.ChunkSize,
                    Allocator.TempJob,
                    NativeArrayOptions.ClearMemory
                );

            try
            {
                // -------------------------------------------------
                // 1. BIOME
                // -------------------------------------------------

                BiomeGenerationJob biomeJob =
                    new BiomeGenerationJob
                    {
                        Context =
                            context,

                        Settings =
                            settings.Biome,

                        BiomeDatabase =
                            biomeDatabase.AsNativeArray(),

                        Output =
                            biomes
                    };

                JobHandle biomeHandle =
                    biomeJob.Schedule();

                biomeHandle.Complete();

                Debug.Log(
                    "[Direct Terrain/Fluid Target] " +
                    "BiomeGenerationJob completado."
                );

                // -------------------------------------------------
                // 2. TERRAIN
                // -------------------------------------------------

                TerrainGenerationJob terrainJob =
                    new TerrainGenerationJob
                    {
                        Context =
                            context,

                        Settings =
                            settings.Terrain,

                        Voxels =
                            voxels,

                        Biomes =
                            biomes,

                        BiomeDatabase =
                            biomeDatabase.AsNativeArray(),

                        SurfaceHeights =
                            surfaceHeights
                    };

                JobHandle terrainHandle =
                    terrainJob.Schedule();

                terrainHandle.Complete();

                // -------------------------------------------------
                // 3. INSPECCIONAR SURFACE HEIGHTS
                // -------------------------------------------------

                int minimumSurface =
                    int.MaxValue;

                int maximumSurface =
                    int.MinValue;

                int columnsBelowSeaLevel = 0;

                int firstLowX = -1;
                int firstLowZ = -1;
                int firstLowSurface = -1;

                for (int z = 0;
                    z < VoxelConstants.ChunkSize;
                    z++)
                {
                    for (int x = 0;
                        x < VoxelConstants.ChunkSize;
                        x++)
                    {
                        int surfaceIndex =
                            x +
                            z *
                            VoxelConstants.ChunkSize;

                        int surface =
                            surfaceHeights[surfaceIndex];

                        if (surface < minimumSurface)
                        {
                            minimumSurface =
                                surface;
                        }

                        if (surface > maximumSurface)
                        {
                            maximumSurface =
                                surface;
                        }

                        if (surface <
                            settings.Terrain.SeaLevel)
                        {
                            columnsBelowSeaLevel++;

                            if (firstLowSurface < 0)
                            {
                                firstLowX = x;
                                firstLowZ = z;
                                firstLowSurface = surface;
                            }
                        }
                    }
                }

                Debug.Log(
                    "[Direct Terrain/Fluid Target] " +
                    $"Terrain terminado. " +
                    $"MinSurface={minimumSurface}, " +
                    $"MaxSurface={maximumSurface}, " +
                    $"ColumnsBelowSeaLevel={columnsBelowSeaLevel}, " +
                    $"FirstLow=({firstLowX},{firstLowZ}), " +
                    $"FirstLowSurface={firstLowSurface}"
                );

                Assert.That(
                    columnsBelowSeaLevel,
                    Is.GreaterThan(0),
                    "El chunk objetivo no contiene ninguna " +
                    "columna por debajo del SeaLevel."
                );

                Assert.That(
                    firstLowSurface,
                    Is.LessThan(
                        settings.Terrain.SeaLevel
                    ),
                    "La primera columna baja no está realmente " +
                    "por debajo del SeaLevel."
                );

                // -------------------------------------------------
                // 4. INSPECCIONAR VOXELS ANTES DEL FLUID
                // -------------------------------------------------

                int expectedWaterStartWorldY =
                    firstLowSurface + 1;

                int expectedWaterEndWorldY =
                    settings.Terrain.SeaLevel;

                int expectedWaterStartLocalY =
                    expectedWaterStartWorldY -
                    context.WorldOrigin.y;

                int expectedWaterEndLocalY =
                    expectedWaterEndWorldY -
                    context.WorldOrigin.y;

                Debug.Log(
                    "[Direct Terrain/Fluid Target] " +
                    $"ExpectedWaterWorldY=" +
                    $"{expectedWaterStartWorldY}.." +
                    $"{expectedWaterEndWorldY}, " +
                    $"ExpectedWaterLocalY=" +
                    $"{expectedWaterStartLocalY}.." +
                    $"{expectedWaterEndLocalY}"
                );

                Assert.That(
                    expectedWaterStartLocalY,
                    Is.GreaterThanOrEqualTo(0),
                    "El agua esperada comienza fuera del límite inferior " +
                    "del chunk."
                );

                Assert.That(
                    expectedWaterEndLocalY,
                    Is.LessThan(
                        VoxelConstants.ChunkSize
                    ),
                    "El agua esperada termina fuera del límite superior " +
                    "del chunk."
                );

                int surfaceIndexForFirstLow =
                    firstLowX +
                    firstLowZ *
                    VoxelConstants.ChunkSize;

                Assert.That(
                    surfaceHeights[
                        surfaceIndexForFirstLow
                    ],
                    Is.EqualTo(firstLowSurface)
                );

                // -------------------------------------------------
                // 5. FLUID
                // -------------------------------------------------

                FluidGenerationJob fluidJob =
                    new FluidGenerationJob
                    {
                        Settings =
                            settings.Fluids,

                        TerrainSettings =
                            settings.Terrain,

                        Voxels =
                            voxels,

                        SurfaceHeights =
                            surfaceHeights,

                        Water =
                            water,

                        WorldOriginY =
                            context.WorldOrigin.y
                    };

                JobHandle fluidHandle =
                    fluidJob.Schedule();

                fluidHandle.Complete();

                // -------------------------------------------------
                // 6. INSPECCIONAR AGUA
                // -------------------------------------------------

                int waterVoxelCount = 0;

                for (int i = 0;
                    i < voxels.Length;
                    i++)
                {
                    if (voxels[i].BlockId !=
                        water.BlockId)
                    {
                        continue;
                    }

                    waterVoxelCount++;
                }

                Debug.Log(
                    "[Direct Terrain/Fluid Target] " +
                    $"Fluid terminado. " +
                    $"WaterVoxels={waterVoxelCount}"
                );

                // -------------------------------------------------
                // 7. INSPECCIONAR EXACTAMENTE LA PRIMERA COLUMNA BAJA
                // -------------------------------------------------

                int columnWaterCount = 0;

                for (int worldY =
                        expectedWaterStartWorldY;
                    worldY <=
                        expectedWaterEndWorldY;
                    worldY++)
                {
                    int localY =
                        worldY -
                        context.WorldOrigin.y;

                    if (localY < 0 ||
                        localY >= VoxelConstants.ChunkSize)
                    {
                        continue;
                    }

                    int voxelIndex =
                        VoxelIndex.ToIndex(
                            firstLowX,
                            localY,
                            firstLowZ
                        );

                    VoxelData voxel =
                        voxels[voxelIndex];

                    Debug.Log(
                        "[Direct Terrain/Fluid Target] " +
                        $"Voxel world=(" +
                        $"{context.WorldOrigin.x + firstLowX}," +
                        $"{worldY}," +
                        $"{context.WorldOrigin.z + firstLowZ}), " +
                        $"local=(" +
                        $"{firstLowX}," +
                        $"{localY}," +
                        $"{firstLowZ}), " +
                        $"BlockId={voxel.BlockId}, " +
                        $"State={voxel.State}, " +
                        $"ExpectedWaterBlockId={water.BlockId}"
                    );

                    if (voxel.BlockId ==
                        water.BlockId)
                    {
                        columnWaterCount++;
                    }
                }

                Debug.Log(
                    "[Direct Terrain/Fluid Target] " +
                    $"FirstLowColumnWaterVoxels=" +
                    $"{columnWaterCount}"
                );

                Assert.That(
                    waterVoxelCount,
                    Is.GreaterThan(0),
                    "TerrainGenerationJob produjo terreno bajo " +
                    "SeaLevel, pero FluidGenerationJob produjo " +
                    "0 voxels de agua."
                );

                Assert.That(
                    columnWaterCount,
                    Is.GreaterThan(0),
                    "FluidGenerationJob no escribió agua ni siquiera " +
                    "en la primera columna cuyo SurfaceHeight está " +
                    "por debajo del SeaLevel."
                );
            }
            finally
            {
                if (voxels.IsCreated)
                    voxels.Dispose();

                if (biomes.IsCreated)
                    biomes.Dispose();

                if (surfaceHeights.IsCreated)
                    surfaceHeights.Dispose();
            }
        }
        finally
        {
            fluidDatabase.Dispose();
            biomeDatabase.Dispose();
        }
    }
    finally
    {
        world.Dispose();
    }
}

[Test]
public void Pipeline_Focused_TerrainChunkThenWaterChunk_GeneratesWater()
{
    ChunkGenerationSettings settings =
        ChunkGenerationSettings.Default;

    settings.Caves.Enabled = false;
    settings.Ores.Enabled = false;
    settings.Fluids.Enabled = true;
    settings.Fluids.GenerateWater = true;

    using VoxelWorld world =
        CreateWorld(settings);

    world.Initialize();

    Chunk terrainChunk =
        world.LoadAndGenerateChunk(
            new ChunkCoordinate(-8, 2, -3)
        );

    world.CompleteGeneration();

    int terrainVoxels = 0;
    int waterVoxels = 0;

    for (int i = 0;
        i < terrainChunk.Data.Voxels.Length;
        i++)
    {
        VoxelData voxel =
            terrainChunk.Data.Voxels[i];

        if (voxel.BlockId != BlockIds.Air &&
            voxel.BlockId != 7)
        {
            terrainVoxels++;
        }

        if (voxel.BlockId == 7)
        {
            waterVoxels++;
        }
    }

    Assert.That(
        terrainChunk.State,
        Is.EqualTo(ChunkState.Generated)
    );

    Assert.That(
        terrainVoxels,
        Is.GreaterThan(0)
    );

    Assert.That(
        waterVoxels,
        Is.GreaterThan(0),
        "El chunk conocido debería contener agua."
    );
}

[Test]
public void Pipeline_Focused_WaterChunk_InspectsTerrainAndFluid()
{
    ChunkGenerationSettings settings =
        ChunkGenerationSettings.Default;

    settings.Caves.Enabled = false;
    settings.Ores.Enabled = false;
    settings.Fluids.Enabled = true;
    settings.Fluids.GenerateWater = true;

    using VoxelWorld world =
        CreateWorld(settings);

    world.Initialize();

    Chunk waterChunk =
        world.LoadAndGenerateChunk(
            new ChunkCoordinate(-8, 2, -3)
        );

    world.CompleteGeneration();

    int airVoxels = 0;
    int solidVoxels = 0;
    int waterVoxels = 0;

    for (int i = 0;
        i < waterChunk.Data.Voxels.Length;
        i++)
    {
        VoxelData voxel =
            waterChunk.Data.Voxels[i];

        if (voxel.BlockId == BlockIds.Air)
        {
            airVoxels++;
        }
        else if (voxel.BlockId == 7)
        {
            waterVoxels++;
        }
        else
        {
            solidVoxels++;
        }
    }

    Assert.That(
        waterChunk.State,
        Is.EqualTo(ChunkState.Generated)
    );

    Assert.That(
        airVoxels,
        Is.GreaterThan(0)
    );

    Assert.That(
        solidVoxels,
        Is.GreaterThan(0)
    );

    Assert.That(
        waterVoxels,
        Is.GreaterThan(0)
    );
}

[Test]
public void Pipeline_Debug_SurfaceHeightIsSameAcrossVerticalChunks()
{
    ChunkGenerationSettings settings =
        ChunkGenerationSettings.Default;

    settings.Caves.Enabled = false;
    settings.Ores.Enabled = false;
    settings.Fluids.Enabled = false;
    settings.Fluids.GenerateWater = false;

    using VoxelWorld world =
        CreateWorld(settings);

    world.Initialize();

    BiomeRuntimeDatabase biomeDatabase =
        new BiomeRuntimeDatabase(
            world.Biomes,
            Allocator.TempJob
        );

    Chunk chunkY0 =
        world.LoadChunk(
            new ChunkCoordinate(-1, 0, -1)
        );

    Chunk chunkY1 =
        world.LoadChunk(
            new ChunkCoordinate(-1, 1, -1)
        );

    ChunkGenerationContext contextY0 =
        new ChunkGenerationContext(
            settings.Seed,
            chunkY0.Coordinate.ToInt3()
        );

    ChunkGenerationContext contextY1 =
        new ChunkGenerationContext(
            settings.Seed,
            chunkY1.Coordinate.ToInt3()
        );

    NativeArray<int> surfaceY0 =
        new NativeArray<int>(
            VoxelConstants.ChunkSize *
            VoxelConstants.ChunkSize,
            Allocator.TempJob
        );

    NativeArray<int> surfaceY1 =
        new NativeArray<int>(
            VoxelConstants.ChunkSize *
            VoxelConstants.ChunkSize,
            Allocator.TempJob
        );

    try
    {
        // =====================================================
        // BIOMES Y0
        // =====================================================

        BiomeGenerationJob biomeJobY0 =
            new BiomeGenerationJob
            {
                Context =
                    contextY0,

                Settings =
                    settings.Biome,

                BiomeDatabase =
                    biomeDatabase.AsNativeArray(),

                Output =
                    chunkY0.BiomeData.Biomes
            };

        JobHandle biomeHandleY0 =
            biomeJobY0.Schedule();

        biomeHandleY0.Complete();

        // =====================================================
        // TERRAIN Y0
        // =====================================================

        TerrainGenerationJob terrainJobY0 =
            new TerrainGenerationJob
            {
                Context =
                    contextY0,

                Settings =
                    settings.Terrain,

                Voxels =
                    chunkY0.Data.Voxels,

                Biomes =
                    chunkY0.BiomeData.Biomes,

                BiomeDatabase =
                    biomeDatabase.AsNativeArray(),

                SurfaceHeights =
                    surfaceY0
            };

        JobHandle terrainHandleY0 =
            terrainJobY0.Schedule();

        terrainHandleY0.Complete();

        // =====================================================
        // BIOMES Y1
        // =====================================================

        BiomeGenerationJob biomeJobY1 =
            new BiomeGenerationJob
            {
                Context =
                    contextY1,

                Settings =
                    settings.Biome,

                BiomeDatabase =
                    biomeDatabase.AsNativeArray(),

                Output =
                    chunkY1.BiomeData.Biomes
            };

        JobHandle biomeHandleY1 =
            biomeJobY1.Schedule();

        biomeHandleY1.Complete();

        // =====================================================
        // TERRAIN Y1
        // =====================================================

        TerrainGenerationJob terrainJobY1 =
            new TerrainGenerationJob
            {
                Context =
                    contextY1,

                Settings =
                    settings.Terrain,

                Voxels =
                    chunkY1.Data.Voxels,

                Biomes =
                    chunkY1.BiomeData.Biomes,

                BiomeDatabase =
                    biomeDatabase.AsNativeArray(),

                SurfaceHeights =
                    surfaceY1
            };

        JobHandle terrainHandleY1 =
            terrainJobY1.Schedule();

        terrainHandleY1.Complete();

        // =====================================================
        // COMPARACIÓN
        // =====================================================

        int minY0 = int.MaxValue;
        int maxY0 = int.MinValue;

        int minY1 = int.MaxValue;
        int maxY1 = int.MinValue;

        int differences = 0;

        int firstDifferenceIndex = -1;

        for (int i = 0;
             i < surfaceY0.Length;
             i++)
        {
            int heightY0 =
                surfaceY0[i];

            int heightY1 =
                surfaceY1[i];

            minY0 =
                Math.Min(
                    minY0,
                    heightY0
                );

            maxY0 =
                Math.Max(
                    maxY0,
                    heightY0
                );

            minY1 =
                Math.Min(
                    minY1,
                    heightY1
                );

            maxY1 =
                Math.Max(
                    maxY1,
                    heightY1
                );

            if (heightY0 != heightY1)
            {
                differences++;

                if (firstDifferenceIndex < 0)
                {
                    firstDifferenceIndex = i;
                }
            }
        }

        Debug.Log(
            "[Surface Height Vertical Debug] " +
            $"Y0={chunkY0.Coordinate}, " +
            $"WorldOriginY={contextY0.WorldOrigin.y}, " +
            $"Min={minY0}, " +
            $"Max={maxY0}; " +
            $"Y1={chunkY1.Coordinate}, " +
            $"WorldOriginY={contextY1.WorldOrigin.y}, " +
            $"Min={minY1}, " +
            $"Max={maxY1}; " +
            $"Differences={differences}"
        );

        if (firstDifferenceIndex >= 0)
        {
            int differenceZ =
                firstDifferenceIndex /
                VoxelConstants.ChunkSize;

            int differenceX =
                firstDifferenceIndex %
                VoxelConstants.ChunkSize;

            Debug.Log(
                "[Surface Height Vertical Debug] " +
                $"First difference index=" +
                $"{firstDifferenceIndex}, " +
                $"local=({differenceX},0,{differenceZ}), " +
                $"Y0={surfaceY0[firstDifferenceIndex]}, " +
                $"Y1={surfaceY1[firstDifferenceIndex]}"
            );
        }

        Assert.That(
            differences,
            Is.EqualTo(0),
            "La altura del terreno cambió entre chunks " +
            "verticales con las mismas coordenadas X/Z."
        );
    }
    finally
    {
        if (surfaceY0.IsCreated)
            surfaceY0.Dispose();

        if (surfaceY1.IsCreated)
            surfaceY1.Dispose();
    }
}

    [Test]
    public void Pipeline_Focused_KnownLowTerrainChunk_GeneratesWater()
    {
        ChunkGenerationSettings settings =
            ChunkGenerationSettings.Default;

        settings.Caves.Enabled = false;
        settings.Ores.Enabled = false;

        settings.Fluids.Enabled = true;
        settings.Fluids.GenerateWater = true;

        using VoxelWorld world =
            CreateWorld(settings);

        world.Initialize();

        Chunk chunk =
            world.LoadAndGenerateChunk(
                new ChunkCoordinate(-8, 2, -3)
            );

        world.CompleteGeneration();

        int waterVoxelCount = 0;
        int solidVoxelCount = 0;
        int airVoxelCount = 0;

        int firstWaterIndex = -1;

        for (int i = 0;
            i < chunk.Data.Voxels.Length;
            i++)
        {
            VoxelData voxel =
                chunk.Data.Voxels[i];

            if (voxel.BlockId == BlockIds.Air)
            {
                airVoxelCount++;
            }
            else if (voxel.BlockId == 7)
            {
                waterVoxelCount++;

                if (firstWaterIndex < 0)
                    firstWaterIndex = i;
            }
            else
            {
                solidVoxelCount++;
            }
        }

        Debug.Log(
            "[Pipeline Known Low Terrain Debug] " +
            $"Chunk={chunk.Coordinate}, " +
            $"WorldOriginY={chunk.Coordinate.Y * VoxelConstants.ChunkSize}, " +
            $"Air={airVoxelCount}, " +
            $"Solid={solidVoxelCount}, " +
            $"Water={waterVoxelCount}, " +
            $"FirstWaterIndex={firstWaterIndex}"
        );

        if (firstWaterIndex >= 0)
        {
            VoxelIndex.FromIndex(
                firstWaterIndex,
                out int localX,
                out int localY,
                out int localZ
            );

            int worldX =
                chunk.Coordinate.X *
                VoxelConstants.ChunkSize +
                localX;

            int worldY =
                chunk.Coordinate.Y *
                VoxelConstants.ChunkSize +
                localY;

            int worldZ =
                chunk.Coordinate.Z *
                VoxelConstants.ChunkSize +
                localZ;

            VoxelData waterVoxel =
                chunk.Data.Voxels[firstWaterIndex];

            Debug.Log(
                "[Pipeline Known Low Terrain Debug] " +
                $"FirstWater local=({localX},{localY},{localZ}), " +
                $"world=({worldX},{worldY},{worldZ}), " +
                $"BlockId={waterVoxel.BlockId}, " +
                $"State={waterVoxel.State}"
            );
        }

        Assert.That(
            chunk.State,
            Is.EqualTo(ChunkState.Generated)
        );

        Assert.That(
            waterVoxelCount,
            Is.GreaterThan(0),
            "El chunk conocido con terreno bajo el SeaLevel " +
            "no generó agua mediante el pipeline."
        );
    }

        private VoxelWorld CreateWorld(
            ChunkGenerationSettings settings)
        {
            VoxelWorldSettings worldSettings =
                new VoxelWorldSettings(
                    initialChunkPoolSize: 4,
                    maximumChunkPoolSize: 32,
                    initialChunkStorageCapacity: 32
                );

            return new VoxelWorld(
                worldSettings,
                settings,
                biomeRegistryAsset,
                blockRegistry,
                oreRegistryAsset,
                fluidRegistryAsset
            );
        }

        private static void GenerateTestChunks(
            VoxelWorld world)
        {
            const int searchRadius = 1;
            const int minChunkY = 0;
            const int maxChunkY = 2;

            for (int y = minChunkY;
                y <= maxChunkY;
                y++)
            {
                for (int z = -searchRadius;
                    z <= searchRadius;
                    z++)
                {
                    for (int x = -searchRadius;
                        x <= searchRadius;
                        x++)
                    {
                        world.LoadAndGenerateChunk(
                            new ChunkCoordinate(
                                x,
                                y,
                                z
                            )
                        );
                    }
                }
            }

            world.CompleteGeneration();
        }

        private static TerrainScanResult ScanTerrain(
            VoxelWorld world)
        {
            return ScanTerrainIgnoringFluids(
                world
            );
        }

        private static TerrainScanResult ScanTerrainIgnoringFluids(
            VoxelWorld world)
        {
            const int searchRadius = 1;
            const int minChunkY = 0;
            const int maxChunkY = 2;

            int minimumWorldHeight =
                int.MaxValue;

            int maximumWorldHeight =
                int.MinValue;

            int columnCount = 0;

            for (int y = minChunkY;
                y <= maxChunkY;
                y++)
            {
                for (int z = -searchRadius;
                    z <= searchRadius;
                    z++)
                {
                    for (int x = -searchRadius;
                        x <= searchRadius;
                        x++)
                    {
                        ChunkCoordinate coordinate =
                            new ChunkCoordinate(
                                x,
                                y,
                                z
                            );

                        if (!world.TryGetChunk(
                                coordinate,
                                out Chunk chunk))
                        {
                            continue;
                        }

                        int chunkSize =
                            VoxelConstants.ChunkSize;

                        for (int localZ = 0;
                            localZ < chunkSize;
                            localZ++)
                        {
                            for (int localX = 0;
                                localX < chunkSize;
                                localX++)
                            {
                                int localY =
                                    FindHighestTerrainVoxel(
                                        chunk,
                                        localX,
                                        localZ
                                    );

                                if (localY < 0)
                                    continue;

                                int worldY =
                                    coordinate.Y *
                                    chunkSize +
                                    localY;

                                columnCount++;

                                if (worldY <
                                    minimumWorldHeight)
                                {
                                    minimumWorldHeight =
                                        worldY;
                                }

                                if (worldY >
                                    maximumWorldHeight)
                                {
                                    maximumWorldHeight =
                                        worldY;
                                }
                            }
                        }
                    }
                }
            }

            return new TerrainScanResult(
                minimumWorldHeight,
                maximumWorldHeight,
                columnCount
            );
        }

        private static int FindHighestTerrainVoxel(
            Chunk chunk,
            int localX,
            int localZ)
        {
            int chunkSize =
                VoxelConstants.ChunkSize;

            for (int localY = chunkSize - 1;
                localY >= 0;
                localY--)
            {
                VoxelData voxel =
                    ChunkDataAccess.GetVoxel(
                        chunk.Data,
                        localX,
                        localY,
                        localZ
                    );

                if (IsTerrainBlock(
                        voxel.BlockId))
                {
                    return localY;
                }
            }

            return -1;
        }

        private static bool IsTerrainBlock(
            ushort blockId)
        {
            return blockId == 1 ||
                   blockId == 2 ||
                   blockId == 3;
        }

        private static int CountWaterVoxelsInChunk(
            Chunk chunk)
        {
            int count = 0;

            for (int i = 0;
                i < chunk.Data.Voxels.Length;
                i++)
            {
                if (chunk.Data.Voxels[i].BlockId == 7)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountWaterVoxels(
            VoxelWorld world)
        {
            const int searchRadius = 1;
            const int minChunkY = 0;
            const int maxChunkY = 2;

            int count = 0;

            for (int y = minChunkY;
                y <= maxChunkY;
                y++)
            {
                for (int z = -searchRadius;
                    z <= searchRadius;
                    z++)
                {
                    for (int x = -searchRadius;
                        x <= searchRadius;
                        x++)
                    {
                        ChunkCoordinate coordinate =
                            new ChunkCoordinate(
                                x,
                                y,
                                z
                            );

                        if (!world.TryGetChunk(
                                coordinate,
                                out Chunk chunk))
                        {
                            continue;
                        }

                        int chunkSize =
                            VoxelConstants.ChunkSize;

                        for (int localZ = 0;
                            localZ < chunkSize;
                            localZ++)
                        {
                            for (int localY = 0;
                                localY < chunkSize;
                                localY++)
                            {
                                for (int localX = 0;
                                    localX < chunkSize;
                                    localX++)
                                {
                                    VoxelData voxel =
                                        ChunkDataAccess.GetVoxel(
                                            chunk.Data,
                                            localX,
                                            localY,
                                            localZ
                                        );

                                    if (voxel.BlockId == 7)
                                    {
                                        count++;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return count;
        }

        private readonly struct TerrainScanResult
        {
            public readonly int MinimumWorldHeight;
            public readonly int MaximumWorldHeight;
            public readonly int ColumnCount;

            public TerrainScanResult(
                int minimumWorldHeight,
                int maximumWorldHeight,
                int columnCount)
            {
                MinimumWorldHeight =
                    minimumWorldHeight;

                MaximumWorldHeight =
                    maximumWorldHeight;

                ColumnCount =
                    columnCount;
            }
        }
    }
}
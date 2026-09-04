using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using WildEarth.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class BiomeGenerationTests
    {
        private NativeArray<BiomeId> biomes;
        private NativeArray<BiomeRuntimeData> biomeDatabase;

        [SetUp]
        public void SetUp()
        {
            biomes =
                new NativeArray<BiomeId>(
                    ChunkBiomeData.Size,
                    Allocator.Persistent
                );

            biomeDatabase =
                new NativeArray<BiomeRuntimeData>(
                    5,
                    Allocator.Persistent
                );

            biomeDatabase[0] =
                CreateBiome(
                    BiomeId.Plains,
                    0.35f,
                    0.75f,
                    0.25f,
                    0.70f
                );

            biomeDatabase[1] =
                CreateBiome(
                    BiomeId.Forest,
                    0.35f,
                    0.80f,
                    0.60f,
                    1.00f
                );

            biomeDatabase[2] =
                CreateBiome(
                    BiomeId.Desert,
                    0.70f,
                    1.00f,
                    0.00f,
                    0.35f
                );

            biomeDatabase[3] =
                CreateBiome(
                    BiomeId.Tundra,
                    0.00f,
                    0.30f,
                    0.00f,
                    1.00f
                );

            biomeDatabase[4] =
                CreateBiome(
                    BiomeId.Mountains,
                    0.00f,
                    0.20f,
                    0.00f,
                    0.20f
                );
        }

        [TearDown]
        public void TearDown()
        {
            if (biomes.IsCreated)
            {
                biomes.Dispose();
            }

            if (biomeDatabase.IsCreated)
            {
                biomeDatabase.Dispose();
            }
        }

        [Test]
        public void BiomeJob_FillsBiomeData()
        {
            ChunkGenerationContext context =
                new ChunkGenerationContext(
                    12345,
                    new int3(
                        0,
                        0,
                        0
                    )
                );

            BiomeGenerationJob job =
                new BiomeGenerationJob
                {
                    Context = context,

                    Settings =
                        BiomeGenerationSettings.Default,

                    BiomeDatabase =
                        biomeDatabase,

                    Output =
                        biomes
                };

            JobHandle handle =
                job.Schedule();

            // Es obligatorio completar el Job antes de
            // leer o liberar cualquiera de sus NativeArrays.
            handle.Complete();

            bool containsValidBiome =
                false;

            for (int i = 0;
                 i < biomes.Length;
                 i++)
            {
                BiomeId biome =
                    biomes[i];

                if (biome >= BiomeId.Plains &&
                    biome <= BiomeId.Mountains)
                {
                    containsValidBiome = true;
                    break;
                }
            }

            Assert.That(
                containsValidBiome,
                Is.True
            );
        }

        [Test]
        public void BiomeJob_IsDeterministic()
        {
            NativeArray<BiomeId> first =
                new NativeArray<BiomeId>(
                    ChunkBiomeData.Size,
                    Allocator.Persistent
                );

            NativeArray<BiomeId> second =
                new NativeArray<BiomeId>(
                    ChunkBiomeData.Size,
                    Allocator.Persistent
                );

            JobHandle firstHandle = default;
            JobHandle secondHandle = default;

            try
            {
                ChunkGenerationContext context =
                    new ChunkGenerationContext(
                        12345,
                        new int3(
                            10,
                            0,
                            -4
                        )
                    );

                BiomeGenerationJob firstJob =
                    new BiomeGenerationJob
                    {
                        Context = context,

                        Settings =
                            BiomeGenerationSettings.Default,

                        BiomeDatabase =
                            biomeDatabase,

                        Output =
                            first
                    };

                BiomeGenerationJob secondJob =
                    new BiomeGenerationJob
                    {
                        Context = context,

                        Settings =
                            BiomeGenerationSettings.Default,

                        BiomeDatabase =
                            biomeDatabase,

                        Output =
                            second
                    };

                firstHandle =
                    firstJob.Schedule();

                /*
                 * Completamos el primer Job ANTES de programar
                 * el segundo. Esto evita que el Test Framework
                 * conserve dependencias pendientes entre Jobs
                 * que utilizan el mismo biomeDatabase.
                 */
                firstHandle.Complete();

                secondHandle =
                    secondJob.Schedule();

                secondHandle.Complete();

                for (int i = 0;
                     i < first.Length;
                     i++)
                {
                    Assert.That(
                        first[i],
                        Is.EqualTo(second[i]),
                        $"El resultado del voxel de bioma {i} no es determinista."
                    );
                }
            }
            finally
            {
                /*
                 * Aunque los Jobs ya deberían estar completos,
                 * completamos nuevamente los handles antes de
                 * liberar los NativeArrays.
                 *
                 * Complete() es seguro aunque el Job ya haya
                 * sido completado.
                 */
                firstHandle.Complete();
                secondHandle.Complete();

                if (first.IsCreated)
                {
                    first.Dispose();
                }

                if (second.IsCreated)
                {
                    second.Dispose();
                }
            }
        }

        private static BiomeRuntimeData CreateBiome(
            BiomeId id,
            float temperatureMin,
            float temperatureMax,
            float moistureMin,
            float moistureMax)
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
                    1f,

                TerrainHeightOffset =
                    0f,

                SurfaceBlockId =
                    3,

                SubSurfaceBlockId =
                    2,

                DeepBlockId =
                    1,

                SubSurfaceDepth =
                    3
            };
        }
    }
}
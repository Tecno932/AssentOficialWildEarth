using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using WildEarth.Voxel;
using VoxelData = WildEarth.Voxel.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class FluidGenerationTests
    {
        private NativeArray<VoxelData> voxels;
        private NativeArray<int> surfaceHeights;

        private FluidRuntimeData water;

        [SetUp]
        public void SetUp()
        {
            voxels =
                new NativeArray<VoxelData>(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Persistent
                );

            surfaceHeights =
                new NativeArray<int>(
                    VoxelConstants.ChunkSize *
                    VoxelConstants.ChunkSize,
                    Allocator.Persistent
                );

            water =
                new FluidRuntimeData
                {
                    Type = FluidType.Water,
                    BlockId = 7,
                    MaxLevel = 15,
                    HorizontalFlowDecay = 1,
                    VerticalFlowDecay = 0,
                    IsLava = false
                };

            /*
             * Por defecto creamos un chunk completamente sólido.
             *
             * Los tests que necesitan aire modificarán
             * específicamente los voxels necesarios.
             */
            for (int i = 0;
                 i < voxels.Length;
                 i++)
            {
                voxels[i] =
                    new VoxelData(1);
            }

            /*
             * Cada columna comienza con una superficie
             * por debajo del SeaLevel.
             *
             * Esto permite comprobar la generación inicial
             * de agua de forma determinista.
             */
            for (int i = 0;
                 i < surfaceHeights.Length;
                 i++)
            {
                surfaceHeights[i] = 30;
            }
        }

        [TearDown]
        public void TearDown()
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

        // ============================================================
        // Disabled
        // ============================================================

        [Test]
        public void Disabled_DoesNotModifyVoxels()
        {
            FluidGenerationSettings settings =
                FluidGenerationSettings.Default;

            settings.Enabled = false;

            PrepareAirColumn(
                x: 0,
                z: 0,
                surfaceY: 30
            );

            RunGeneration(
                settings,
                new int3(0, 0, 0)
            );

            for (int y = 0; y < VoxelConstants.ChunkSize; y++)
            {
                int index =
                    VoxelIndex.ToIndex(
                        0,
                        y,
                        0
                    );

                Assert.That(
                    voxels[index].BlockId,
                    Is.EqualTo(BlockIds.Air),
                    $"El voxel Y={y} fue modificado aunque la generación estaba desactivada."
                );
            }
        }

        // ============================================================
        // GenerateWater disabled
        // ============================================================

        [Test]
        public void GenerateWaterDisabled_DoesNotModifyVoxels()
        {
            FluidGenerationSettings settings =
                FluidGenerationSettings.Default;

            settings.GenerateWater = false;

            PrepareAirColumn(
                x: 0,
                z: 0,
                surfaceY: 30
            );

            RunGeneration(
                settings,
                new int3(0, 0, 0)
            );

            for (int y = 0; y < VoxelConstants.ChunkSize; y++)
            {
                int index =
                    VoxelIndex.ToIndex(
                        0,
                        y,
                        0
                    );

                Assert.That(
                    voxels[index].BlockId,
                    Is.EqualTo(BlockIds.Air),
                    $"El voxel Y={y} fue modificado aunque GenerateWater estaba desactivado."
                );
            }
        }

        // ============================================================
        // Water generation
        // ============================================================

        [Test]
        public void GeneratesWaterBelowSeaLevel()
        {
            PrepareAirColumn(
                x: 0,
                z: 0,
                surfaceY: 5
            );

            RunGeneration(
                FluidGenerationSettings.Default,
                new int3(0, 0, 0)
            );

            /*
             * SeaLevel = 40.
             *
             * La columna tiene superficie en Y=5.
             *
             * El agua debe ocupar:
             *
             * Y=6 ... Y=15
             *
             * dentro de este chunk.
             */
            for (int localY = 0;
                 localY < VoxelConstants.ChunkSize;
                 localY++)
            {
                int index =
                    VoxelIndex.ToIndex(
                        0,
                        localY,
                        0
                    );

                if (localY >= 6)
                {
                    Assert.That(
                        voxels[index].BlockId,
                        Is.EqualTo(water.BlockId),
                        $"No se generó agua en Y={localY}."
                    );

                    Assert.That(
                        voxels[index].State,
                        Is.EqualTo(water.MaxLevel),
                        $"El nivel del agua en Y={localY} no es MaxLevel."
                    );
                }
            }
        }

        [Test]
        public void DoesNotGenerateWaterAboveOrAtTerrainSurface()
        {
            const int surfaceY = 10;

            PrepareAirColumn(
                x: 0,
                z: 0,
                surfaceY: surfaceY
            );

            RunGeneration(
                FluidGenerationSettings.Default,
                new int3(0, 0, 0)
            );

            /*
             * Y=10 es la superficie.
             * El agua comienza en Y=11.
             */
            int surfaceIndex =
                VoxelIndex.ToIndex(
                    0,
                    surfaceY,
                    0
                );

            Assert.That(
                voxels[surfaceIndex].BlockId,
                Is.EqualTo(BlockIds.Air)
            );

            int aboveIndex =
                VoxelIndex.ToIndex(
                    0,
                    surfaceY + 1,
                    0
                );

            Assert.That(
                voxels[aboveIndex].BlockId,
                Is.EqualTo(water.BlockId)
            );
        }

        // ============================================================
        // Sea level
        // ============================================================

        [Test]
        public void TerrainAtSeaLevel_DoesNotGenerateWater()
        {
            const int surfaceY = 40;

            PrepareAirColumn(
                x: 0,
                z: 0,
                surfaceY: surfaceY
            );

            RunGeneration(
                FluidGenerationSettings.Default,
                new int3(0, 0, 0)
            );

            for (int y = 0;
                 y < VoxelConstants.ChunkSize;
                 y++)
            {
                int index =
                    VoxelIndex.ToIndex(
                        0,
                        y,
                        0
                    );

                Assert.That(
                    voxels[index].BlockId,
                    Is.EqualTo(BlockIds.Air)
                );
            }
        }

        [Test]
        public void TerrainAboveSeaLevel_DoesNotGenerateWater()
        {
            const int surfaceY = 60;

            PrepareAirColumn(
                x: 0,
                z: 0,
                surfaceY: surfaceY
            );

            RunGeneration(
                FluidGenerationSettings.Default,
                new int3(0, 0, 0)
            );

            for (int y = 0;
                 y < VoxelConstants.ChunkSize;
                 y++)
            {
                int index =
                    VoxelIndex.ToIndex(
                        0,
                        y,
                        0
                    );

                Assert.That(
                    voxels[index].BlockId,
                    Is.EqualTo(BlockIds.Air)
                );
            }
        }

        // ============================================================
        // Solid blocks
        // ============================================================

        [Test]
        public void DoesNotReplaceSolidBlocks()
        {
            const int surfaceY = 5;

            /*
             * La columna completa es sólida.
             * No debe aparecer agua porque el Job solamente
             * reemplaza voxels que ya sean Air.
             */
            surfaceHeights[0] = surfaceY;

            RunGeneration(
                FluidGenerationSettings.Default,
                new int3(0, 0, 0)
            );

            for (int y = 0;
                 y < VoxelConstants.ChunkSize;
                 y++)
            {
                int index =
                    VoxelIndex.ToIndex(
                        0,
                        y,
                        0
                    );

                Assert.That(
                    voxels[index].BlockId,
                    Is.EqualTo(1),
                    $"El bloque sólido Y={y} fue reemplazado por agua."
                );
            }
        }

        // ============================================================
        // Determinism
        // ============================================================

        [Test]
        public void SameInputProducesSameResult()
        {
            NativeArray<VoxelData> first =
                CreateAirChunk();

            NativeArray<VoxelData> second =
                CreateAirChunk();

            JobHandle firstHandle = default;
            JobHandle secondHandle = default;

            try
            {
                PrepareAirColumn(
                    first,
                    0,
                    0
                );

                PrepareAirColumn(
                    second,
                    0,
                    0
                );

                FluidGenerationJob firstJob =
                    CreateJob(
                        first
                    );

                FluidGenerationJob secondJob =
                    CreateJob(
                        second
                    );

                firstHandle =
                    firstJob.Schedule();

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
                        $"El voxel {i} no fue determinista."
                    );
                }
            }
            finally
            {
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

        // ============================================================
        // Chunk vertical position
        // ============================================================

        [Test]
        public void WorldOriginYIsUsedToResolveLocalY()
        {
            /*
             * El chunk comienza en Y=32.
             *
             * Surface = Y=30.
             * SeaLevel = Y=40.
             *
             * El agua debe comenzar en Y=31,
             * que corresponde a localY= -1 y por lo tanto
             * no pertenece a este chunk.
             *
             * En este chunk solamente deben aparecer
             * aguas desde el rango mundial que intersecte
             * [32, 47].
             */

            NativeArray<VoxelData> verticalChunk =
                new NativeArray<VoxelData>(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Persistent
                );

            JobHandle handle = default;

            try
            {
                for (int i = 0;
                     i < verticalChunk.Length;
                     i++)
                {
                    verticalChunk[i] =
                        new VoxelData(BlockIds.Air);
                }

                FluidGenerationJob job =
                    CreateJob(
                        verticalChunk
                    );

                job.WorldOriginY = 32;

                handle =
                    job.Schedule();

                handle.Complete();

                /*
                 * Como el agua mundial empieza en Y=31,
                 * el primer voxel de este chunk, Y=32,
                 * debe contener agua.
                 */
                int firstIndex =
                    VoxelIndex.ToIndex(
                        0,
                        0,
                        0
                    );

                Assert.That(
                    verticalChunk[firstIndex].BlockId,
                    Is.EqualTo(water.BlockId)
                );
            }
            finally
            {
                handle.Complete();

                if (verticalChunk.IsCreated)
                {
                    verticalChunk.Dispose();
                }
            }
        }

        // ============================================================
        // Helpers
        // ============================================================

        private void RunGeneration(
            FluidGenerationSettings settings,
            int3 chunkCoordinate)
        {
            FluidGenerationJob job =
                CreateJob(
                    voxels
                );

            job.Settings =
                settings;

            job.WorldOriginY =
                chunkCoordinate.y *
                VoxelConstants.ChunkSize;

            JobHandle handle =
                job.Schedule();

            handle.Complete();
        }

        private FluidGenerationJob CreateJob(
            NativeArray<VoxelData> targetVoxels)
        {
            return new FluidGenerationJob
            {
                Settings =
                    FluidGenerationSettings.Default,

                TerrainSettings =
                    TerrainGenerationSettings.Default,

                Voxels =
                    targetVoxels,

                SurfaceHeights =
                    surfaceHeights,

                Water =
                    water,

                WorldOriginY = 0
            };
        }

        private void PrepareAirColumn(
            int x,
            int z,
            int surfaceY)
        {
            PrepareAirColumn(
                voxels,
                x,
                z
            );

            surfaceHeights[
                x +
                z * VoxelConstants.ChunkSize
            ] = surfaceY;
        }

        private static void PrepareAirColumn(
            NativeArray<VoxelData> target,
            int x,
            int z)
        {
            for (int y = 0;
                 y < VoxelConstants.ChunkSize;
                 y++)
            {
                int index =
                    VoxelIndex.ToIndex(
                        x,
                        y,
                        z
                    );

                target[index] =
                    new VoxelData(BlockIds.Air);
            }
        }

        private static NativeArray<VoxelData>
            CreateAirChunk()
        {
            NativeArray<VoxelData> result =
                new NativeArray<VoxelData>(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Persistent
                );

            for (int i = 0;
                 i < result.Length;
                 i++)
            {
                result[i] =
                    new VoxelData(BlockIds.Air);
            }

            return result;
        }
    }
}
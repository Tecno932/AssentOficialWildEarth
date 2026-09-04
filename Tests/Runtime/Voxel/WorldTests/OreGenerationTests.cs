using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using WildEarth.Voxel;
using VoxelData = WildEarth.Voxel.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class OreGenerationTests
    {
        private NativeArray<OreRuntimeData> oreDatabase;
        private NativeArray<ushort> hostBlockIds;

        [SetUp]
        public void SetUp()
        {
            oreDatabase =
                new NativeArray<OreRuntimeData>(
                    1,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory
                );

            hostBlockIds =
                new NativeArray<ushort>(
                    1,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory
                );

            // Stone = bloque huésped.
            hostBlockIds[0] = 1;

            oreDatabase[0] =
                new OreRuntimeData
                {
                    BlockId = 4, // Copper Ore

                    MinY = 5,
                    MaxY = 110,

                    Rarity = 0.20f,

                    MinVeinSize = 3,
                    MaxVeinSize = 6,

                    Frequency = 0.05f,

                    HostBlockStart = 0,
                    HostBlockCount = 1
                };
        }

        [TearDown]
        public void TearDown()
        {
            if (oreDatabase.IsCreated)
                oreDatabase.Dispose();

            if (hostBlockIds.IsCreated)
                hostBlockIds.Dispose();
        }

        // ============================================================
        // Disabled
        // ============================================================

        [Test]
        public void DisabledDoesNothing()
        {
            NativeArray<VoxelData> voxels =
                CreateStoneChunk();

            try
            {
                RunGeneration(
                    voxels,
                    seed: 12345,
                    chunkCoordinate: new int3(0, 0, 0),
                    enabled: false
                );

                for (int i = 0; i < voxels.Length; i++)
                {
                    Assert.That(
                        voxels[i].BlockId,
                        Is.EqualTo(1),
                        $"El voxel {i} fue modificado aunque la generación estaba desactivada."
                    );
                }
            }
            finally
            {
                voxels.Dispose();
            }
        }

        // ============================================================
        // Determinism
        // ============================================================

        [Test]
        public void SameSeedProducesSameResult()
        {
            NativeArray<VoxelData> voxelsA =
                CreateStoneChunk();

            NativeArray<VoxelData> voxelsB =
                CreateStoneChunk();

            try
            {
                RunGeneration(
                    voxelsA,
                    seed: 12345,
                    chunkCoordinate: new int3(0, 1, 0),
                    enabled: true
                );

                RunGeneration(
                    voxelsB,
                    seed: 12345,
                    chunkCoordinate: new int3(0, 1, 0),
                    enabled: true
                );

                Assert.That(
                    voxelsA.Length,
                    Is.EqualTo(voxelsB.Length)
                );

                for (int i = 0; i < voxelsA.Length; i++)
                {
                    Assert.That(
                        voxelsA[i],
                        Is.EqualTo(voxelsB[i]),
                        $"El voxel {i} no fue determinista."
                    );
                }
            }
            finally
            {
                voxelsA.Dispose();
                voxelsB.Dispose();
            }
        }

        // ============================================================
        // Different seeds
        // ============================================================

        [Test]
        public void DifferentSeedProducesDifferentResult()
        {
            NativeArray<VoxelData> voxelsA =
                CreateStoneChunk();

            NativeArray<VoxelData> voxelsB =
                CreateStoneChunk();

            try
            {
                RunGeneration(
                    voxelsA,
                    seed: 12345,
                    chunkCoordinate: new int3(0, 1, 0),
                    enabled: true
                );

                RunGeneration(
                    voxelsB,
                    seed: 98765,
                    chunkCoordinate: new int3(0, 1, 0),
                    enabled: true
                );

                bool different = false;

                for (int i = 0; i < voxelsA.Length; i++)
                {
                    if (voxelsA[i] != voxelsB[i])
                    {
                        different = true;
                        break;
                    }
                }

                Assert.That(
                    different,
                    Is.True,
                    "Dos seeds diferentes produjeron exactamente el mismo chunk."
                );
            }
            finally
            {
                voxelsA.Dispose();
                voxelsB.Dispose();
            }
        }

        // ============================================================
        // Host blocks
        // ============================================================

        [Test]
        public void OnlyHostBlocksAreReplaced()
        {
            NativeArray<VoxelData> voxels =
                CreateMixedChunk();

            NativeArray<VoxelData> before =
                new NativeArray<VoxelData>(
                    voxels.Length,
                    Allocator.TempJob
                );

            try
            {
                NativeArray<VoxelData>.Copy(
                    voxels,
                    before
                );

                // Rarity 1 garantiza que la prueba tenga candidatos
                // suficientes para comprobar el reemplazo.
                OreRuntimeData testOre =
                    oreDatabase[0];

                testOre.Rarity = 1f;
                testOre.MinVeinSize = 1;
                testOre.MaxVeinSize = 1;

                oreDatabase[0] = testOre;

                RunGeneration(
                    voxels,
                    seed: 12345,
                    chunkCoordinate: new int3(0, 1, 0),
                    enabled: true
                );

                bool foundOre = false;

                for (int i = 0; i < voxels.Length; i++)
                {
                    ushort original =
                        before[i].BlockId;

                    ushort result =
                        voxels[i].BlockId;

                    // Dirt nunca puede ser reemplazada porque
                    // el único bloque huésped es Stone.
                    if (original == 2)
                    {
                        Assert.That(
                            result,
                            Is.EqualTo(2),
                            $"El voxel {i} era Dirt y fue reemplazado."
                        );
                    }

                    // Si apareció mineral, originalmente debía
                    // existir Stone.
                    if (result == 4)
                    {
                        Assert.That(
                            original,
                            Is.EqualTo(1),
                            $"El mineral del voxel {i} no reemplazó Stone."
                        );

                        foundOre = true;
                    }
                }

                Assert.That(
                    foundOre,
                    Is.True,
                    "No se generó ningún mineral durante la prueba de bloques huésped."
                );
            }
            finally
            {
                before.Dispose();
                voxels.Dispose();
            }
        }

        // ============================================================
        // Y range
        // ============================================================

        [Test]
        public void OreNeverGeneratesOutsideYRange()
        {
            /*
             * El chunk empieza en Y = 64:
             *
             * ChunkCoordinate Y = 4
             * 4 * 16 = 64
             *
             * Configuramos el mineral para que solo pueda aparecer
             * entre Y = 70 y Y = 72.
             */

            OreRuntimeData testOre =
                oreDatabase[0];

            testOre.MinY = 70;
            testOre.MaxY = 72;

            // Forzamos generación para que la prueba
            // no dependa de una probabilidad baja.
            testOre.Rarity = 1f;
            testOre.MinVeinSize = 1;
            testOre.MaxVeinSize = 1;

            oreDatabase[0] = testOre;

            NativeArray<VoxelData> voxels =
                CreateStoneChunk();

            try
            {
                RunGeneration(
                    voxels,
                    seed: 12345,
                    chunkCoordinate: new int3(0, 4, 0),
                    enabled: true
                );

                bool foundOre = false;

                for (int index = 0;
                     index < voxels.Length;
                     index++)
                {
                    VoxelData voxel =
                        voxels[index];

                    if (voxel.BlockId != 4)
                        continue;

                    VoxelIndex.FromIndex(
                        index,
                        out _,
                        out int localY,
                        out _
                    );

                    int worldY =
                        64 + localY;

                    Assert.That(
                        worldY,
                        Is.GreaterThanOrEqualTo(70),
                        $"El mineral apareció por debajo del rango permitido: Y={worldY}."
                    );

                    Assert.That(
                        worldY,
                        Is.LessThanOrEqualTo(72),
                        $"El mineral apareció por encima del rango permitido: Y={worldY}."
                    );

                    foundOre = true;
                }

                Assert.That(
                    foundOre,
                    Is.True,
                    "No se generó ningún mineral dentro del rango Y configurado."
                );
            }
            finally
            {
                voxels.Dispose();
            }
        }

        // ============================================================
        // Chunk position
        // ============================================================

        [Test]
        public void ChunkCoordinateAffectsGenerationPosition()
        {
            NativeArray<VoxelData> voxelsA =
                CreateStoneChunk();

            NativeArray<VoxelData> voxelsB =
                CreateStoneChunk();

            try
            {
                RunGeneration(
                    voxelsA,
                    seed: 12345,
                    chunkCoordinate: new int3(0, 1, 0),
                    enabled: true
                );

                RunGeneration(
                    voxelsB,
                    seed: 12345,
                    chunkCoordinate: new int3(10, 1, 10),
                    enabled: true
                );

                bool different = false;

                for (int i = 0; i < voxelsA.Length; i++)
                {
                    if (voxelsA[i] != voxelsB[i])
                    {
                        different = true;
                        break;
                    }
                }

                Assert.That(
                    different,
                    Is.True,
                    "Chunks en posiciones mundiales diferentes produjeron exactamente el mismo resultado."
                );
            }
            finally
            {
                voxelsA.Dispose();
                voxelsB.Dispose();
            }
        }

        // ============================================================
        // Helpers
        // ============================================================

        private void RunGeneration(
            NativeArray<VoxelData> voxels,
            int seed,
            int3 chunkCoordinate,
            bool enabled)
        {
            OreGenerationJob job =
                new OreGenerationJob
                {
                    Context =
                        new ChunkGenerationContext(
                            seed,
                            chunkCoordinate
                        ),

                    Settings =
                        CreateSettings(
                            enabled
                        ),

                    Voxels = voxels,

                    OreDatabase =
                        oreDatabase,

                    HostBlockIds =
                        hostBlockIds
                };

            JobHandle handle =
                job.Schedule();

            handle.Complete();
        }

        private static OreGenerationSettings CreateSettings(
            bool enabled)
        {
            OreGenerationSettings settings =
                OreGenerationSettings.Default;

            settings.Enabled = enabled;

            return settings;
        }

        private static NativeArray<VoxelData>
            CreateStoneChunk()
        {
            NativeArray<VoxelData> voxels =
                new NativeArray<VoxelData>(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.TempJob,
                    NativeArrayOptions.UninitializedMemory
                );

            for (int i = 0; i < voxels.Length; i++)
            {
                voxels[i] =
                    new VoxelData(
                        1 // Stone
                    );
            }

            return voxels;
        }

        private static NativeArray<VoxelData>
            CreateMixedChunk()
        {
            NativeArray<VoxelData> voxels =
                new NativeArray<VoxelData>(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.TempJob,
                    NativeArrayOptions.UninitializedMemory
                );

            for (int i = 0; i < voxels.Length; i++)
            {
                voxels[i] =
                    new VoxelData(
                        i % 3 == 0
                            ? (ushort)2 // Dirt
                            : (ushort)1 // Stone
                    );
            }

            return voxels;
        }
    }
}
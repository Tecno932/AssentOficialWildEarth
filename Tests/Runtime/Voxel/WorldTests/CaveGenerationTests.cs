using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using WildEarth.Voxel;
using VoxelData = WildEarth.Voxel.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class CaveGenerationTests
    {
        private NativeArray<VoxelData> voxels;
        private NativeArray<BlockRuntimeData> blockDatabase;

        [SetUp]
        public void SetUp()
        {
            voxels =
                new NativeArray<VoxelData>(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Persistent
                );

            blockDatabase =
                new NativeArray<BlockRuntimeData>(
                    2,
                    Allocator.Persistent
                );

            blockDatabase[0] =
                new BlockRuntimeData
                {
                    Id = BlockIds.Air,
                    Flags = BlockFlags.None
                };

            blockDatabase[1] =
                new BlockRuntimeData
                {
                    Id = 1,
                    Flags =
                        BlockFlags.Solid |
                        BlockFlags.CaveCarvable
                };

            for (int i = 0;
                 i < voxels.Length;
                 i++)
            {
                voxels[i] =
                    new VoxelData(1);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (voxels.IsCreated)
            {
                voxels.Dispose();
            }

            if (blockDatabase.IsCreated)
            {
                blockDatabase.Dispose();
            }
        }

        [Test]
        public void CaveJob_Disabled_DoesNotModifyVoxels()
        {
            CaveGenerationSettings settings =
                CaveGenerationSettings.Default;

            settings.Enabled = false;

            ChunkGenerationContext context =
                new ChunkGenerationContext(
                    12345,
                    new int3(
                        0,
                        0,
                        0
                    )
                );

            CaveGenerationJob job =
                new CaveGenerationJob
                {
                    Context = context,

                    Settings = settings,

                    Voxels = voxels,

                    BlockDatabase =
                        blockDatabase
                };

            JobHandle handle =
                job.Schedule();

            handle.Complete();

            for (int i = 0;
                 i < voxels.Length;
                 i++)
            {
                Assert.That(
                    voxels[i].BlockId,
                    Is.EqualTo(1)
                );
            }
        }

        [Test]
        public void CaveJob_WithImpossibleThreshold_DoesNotModifyVoxels()
        {
            CaveGenerationSettings settings =
                CaveGenerationSettings.Default;

            settings.Threshold = 1.1f;

            ChunkGenerationContext context =
                new ChunkGenerationContext(
                    12345,
                    new int3(
                        0,
                        0,
                        0
                    )
                );

            CaveGenerationJob job =
                new CaveGenerationJob
                {
                    Context = context,

                    Settings = settings,

                    Voxels = voxels,

                    BlockDatabase =
                        blockDatabase
                };

            JobHandle handle =
                job.Schedule();

            handle.Complete();

            for (int i = 0;
                 i < voxels.Length;
                 i++)
            {
                Assert.That(
                    voxels[i].BlockId,
                    Is.EqualTo(1)
                );
            }
        }

        [Test]
        public void CaveJob_IsDeterministic()
        {
            NativeArray<VoxelData> first =
                new NativeArray<VoxelData>(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Persistent
                );

            NativeArray<VoxelData> second =
                new NativeArray<VoxelData>(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.Persistent
                );

            JobHandle firstHandle = default;
            JobHandle secondHandle = default;

            try
            {
                for (int i = 0;
                     i < first.Length;
                     i++)
                {
                    first[i] =
                        new VoxelData(1);

                    second[i] =
                        new VoxelData(1);
                }

                CaveGenerationSettings settings =
                    CaveGenerationSettings.Default;

                ChunkGenerationContext context =
                    new ChunkGenerationContext(
                        12345,
                        new int3(
                            10,
                            0,
                            -4
                        )
                    );

                CaveGenerationJob firstJob =
                    new CaveGenerationJob
                    {
                        Context = context,

                        Settings = settings,

                        Voxels = first,

                        BlockDatabase =
                            blockDatabase
                    };

                CaveGenerationJob secondJob =
                    new CaveGenerationJob
                    {
                        Context = context,

                        Settings = settings,

                        Voxels = second,

                        BlockDatabase =
                            blockDatabase
                    };

                firstHandle =
                    firstJob.Schedule();

                /*
                 * Completamos completamente el primer Job
                 * antes de iniciar el segundo.
                 *
                 * Ambos Jobs usan el mismo blockDatabase.
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
                        first[i].BlockId,
                        Is.EqualTo(
                            second[i].BlockId
                        ),
                        $"El voxel {i} no produjo un resultado determinista."
                    );
                }
            }
            finally
            {
                /*
                 * Garantizamos que ningún Job siga utilizando
                 * los NativeArrays antes de hacer Dispose().
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

        [Test]
        public void CaveJob_CanCreateAir()
        {
            CaveGenerationSettings settings =
                CaveGenerationSettings.Default;

            settings.Threshold = 0.1f;

            ChunkGenerationContext context =
                new ChunkGenerationContext(
                    12345,
                    new int3(
                        0,
                        0,
                        0
                    )
                );

            CaveGenerationJob job =
                new CaveGenerationJob
                {
                    Context = context,

                    Settings = settings,

                    Voxels = voxels,

                    BlockDatabase =
                        blockDatabase
                };

            JobHandle handle =
                job.Schedule();

            handle.Complete();

            bool foundAir = false;

            for (int i = 0;
                 i < voxels.Length;
                 i++)
            {
                if (voxels[i].BlockId ==
                    BlockIds.Air)
                {
                    foundAir = true;
                    break;
                }
            }

            Assert.That(
                foundAir,
                Is.True
            );
        }

        [Test]
        public void CaveJob_DoesNotCarveProtectedBlocks()
        {
            NativeArray<BlockRuntimeData> protectedDatabase =
                new NativeArray<BlockRuntimeData>(
                    2,
                    Allocator.Persistent
                );

            JobHandle handle = default;

            try
            {
                protectedDatabase[0] =
                    new BlockRuntimeData
                    {
                        Id = BlockIds.Air,
                        Flags = BlockFlags.None
                    };

                protectedDatabase[1] =
                    new BlockRuntimeData
                    {
                        Id = 1,
                        Flags = BlockFlags.Solid
                    };

                CaveGenerationSettings settings =
                    CaveGenerationSettings.Default;

                settings.Threshold = 0.1f;

                ChunkGenerationContext context =
                    new ChunkGenerationContext(
                        12345,
                        new int3(
                            0,
                            0,
                            0
                        )
                    );

                CaveGenerationJob job =
                    new CaveGenerationJob
                    {
                        Context = context,

                        Settings = settings,

                        Voxels = voxels,

                        BlockDatabase =
                            protectedDatabase
                    };

                handle =
                    job.Schedule();

                handle.Complete();

                for (int i = 0;
                     i < voxels.Length;
                     i++)
                {
                    Assert.That(
                        voxels[i].BlockId,
                        Is.EqualTo(1)
                    );
                }
            }
            finally
            {
                /*
                 * El database también pertenece al Job,
                 * por lo que debe estar completado antes
                 * de hacer Dispose().
                 */
                handle.Complete();

                if (protectedDatabase.IsCreated)
                {
                    protectedDatabase.Dispose();
                }
            }
        }
    }
}
using NUnit.Framework;
using Unity.Collections;

namespace WildEarth.Voxel.Tests
{
    public sealed class FluidSimulationJobTests
    {
        private const ushort WaterBlockId = 10;

        private NativeArray<Voxel> voxels;
        private NativeArray<FluidRuntimeData> fluidsByBlockId;
        private NativeArray<FluidChange> changes;
        private NativeArray<byte> changeCounts;

        [SetUp]
        public void SetUp()
        {
            voxels =
                new NativeArray<Voxel>(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.TempJob,
                    NativeArrayOptions.ClearMemory
                );

            fluidsByBlockId =
                new NativeArray<FluidRuntimeData>(
                    WaterBlockId + 1,
                    Allocator.TempJob,
                    NativeArrayOptions.ClearMemory
                );

            changes =
                new NativeArray<FluidChange>(
                    VoxelConstants.VoxelsPerChunk *
                    FluidChangeBuffer.MaxChangesPerVoxel,
                    Allocator.TempJob,
                    NativeArrayOptions.UninitializedMemory
                );

            changeCounts =
                new NativeArray<byte>(
                    VoxelConstants.VoxelsPerChunk,
                    Allocator.TempJob,
                    NativeArrayOptions.ClearMemory
                );

            fluidsByBlockId[WaterBlockId] =
                new FluidRuntimeData
                {
                    Type = FluidType.Water,
                    BlockId = WaterBlockId,
                    MaxLevel = FluidState.MaxLevel,
                    HorizontalFlowDecay = 1,
                    VerticalFlowDecay = 0,
                    IsLava = false
                };
        }

        [TearDown]
        public void TearDown()
        {
            if (voxels.IsCreated)
                voxels.Dispose();

            if (fluidsByBlockId.IsCreated)
                fluidsByBlockId.Dispose();

            if (changes.IsCreated)
                changes.Dispose();

            if (changeCounts.IsCreated)
                changeCounts.Dispose();
        }

        [Test]
        public void NonFluidVoxelProducesNoChanges()
        {
            int sourceIndex =
                VoxelIndex.ToIndex(
                    8,
                    8,
                    8
                );

            voxels[sourceIndex] =
                new Voxel(
                    BlockIds.Air
                );

            RunJob();

            Assert.AreEqual(
                0,
                changeCounts[sourceIndex]
            );
        }

        [Test]
        public void FluidSourceProducesVerticalChange()
        {
            int sourceIndex =
                VoxelIndex.ToIndex(
                    8,
                    8,
                    8
                );

            voxels[sourceIndex] =
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                );

            RunJob();

            int count =
                changeCounts[sourceIndex];

            Assert.GreaterOrEqual(
                count,
                1
            );

            FluidChange vertical =
                FindChange(
                    sourceIndex,
                    8,
                    7,
                    8
                );

            Assert.AreEqual(
                FluidType.Water,
                vertical.State.Type
            );

            Assert.AreEqual(
                FluidState.MaxLevel,
                vertical.State.Level
            );
        }

        [Test]
        public void FluidSourceProducesHorizontalChanges()
        {
            int sourceIndex =
                VoxelIndex.ToIndex(
                    8,
                    8,
                    8
                );

            voxels[sourceIndex] =
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                );

            RunJob();

            AssertChangeExists(
                sourceIndex,
                9,
                8,
                8
            );

            AssertChangeExists(
                sourceIndex,
                7,
                8,
                8
            );

            AssertChangeExists(
                sourceIndex,
                8,
                8,
                9
            );

            AssertChangeExists(
                sourceIndex,
                8,
                8,
                7
            );
        }

        [Test]
        public void HorizontalFlowUsesConfiguredDecay()
        {
            fluidsByBlockId[WaterBlockId] =
                new FluidRuntimeData
                {
                    Type = FluidType.Water,
                    BlockId = WaterBlockId,
                    MaxLevel = FluidState.MaxLevel,
                    HorizontalFlowDecay = 3,
                    VerticalFlowDecay = 0,
                    IsLava = false
                };

            int sourceIndex =
                VoxelIndex.ToIndex(
                    8,
                    8,
                    8
                );

            voxels[sourceIndex] =
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                );

            RunJob();

            FluidChange change =
                FindChange(
                    sourceIndex,
                    9,
                    8,
                    8
                );

            Assert.AreEqual(
                12,
                change.State.Level
            );
        }

        [Test]
        public void VerticalFlowUsesConfiguredDecay()
        {
            fluidsByBlockId[WaterBlockId] =
                new FluidRuntimeData
                {
                    Type = FluidType.Water,
                    BlockId = WaterBlockId,
                    MaxLevel = FluidState.MaxLevel,
                    HorizontalFlowDecay = 1,
                    VerticalFlowDecay = 4,
                    IsLava = false
                };

            int sourceIndex =
                VoxelIndex.ToIndex(
                    8,
                    8,
                    8
                );

            voxels[sourceIndex] =
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                );

            RunJob();

            FluidChange change =
                FindChange(
                    sourceIndex,
                    8,
                    7,
                    8
                );

            Assert.AreEqual(
                11,
                change.State.Level
            );
        }

        [Test]
        public void SolidVoxelBlocksVerticalFlow()
        {
            int sourceIndex =
                VoxelIndex.ToIndex(
                    8,
                    8,
                    8
                );

            int targetIndex =
                VoxelIndex.ToIndex(
                    8,
                    7,
                    8
                );

            voxels[sourceIndex] =
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                );

            voxels[targetIndex] =
                new Voxel(
                    1
                );

            RunJob();

            AssertChangeDoesNotExist(
                sourceIndex,
                8,
                7,
                8
            );
        }

        [Test]
        public void SolidVoxelBlocksHorizontalFlow()
        {
            int sourceIndex =
                VoxelIndex.ToIndex(
                    8,
                    8,
                    8
                );

            int targetIndex =
                VoxelIndex.ToIndex(
                    9,
                    8,
                    8
                );

            voxels[sourceIndex] =
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                );

            voxels[targetIndex] =
                new Voxel(
                    1
                );

            RunJob();

            AssertChangeDoesNotExist(
                sourceIndex,
                9,
                8,
                8
            );
        }

        [Test]
        public void BoundaryDoesNotCreateInvalidHorizontalChanges()
        {
            int sourceIndex =
                VoxelIndex.ToIndex(
                    0,
                    8,
                    8
                );

            voxels[sourceIndex] =
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                );

            RunJob();

            AssertChangeDoesNotExist(
                sourceIndex,
                -1,
                8,
                8
            );

            AssertChangeExists(
                sourceIndex,
                1,
                8,
                8
            );

            AssertChangeExists(
                sourceIndex,
                0,
                7,
                8
            );
        }

        [Test]
        public void MaximumChangesPerVoxelIsFive()
        {
            int sourceIndex =
                VoxelIndex.ToIndex(
                    8,
                    8,
                    8
                );

            voxels[sourceIndex] =
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                );

            RunJob();

            Assert.LessOrEqual(
                changeCounts[sourceIndex],
                FluidChangeBuffer.MaxChangesPerVoxel
            );

            Assert.AreEqual(
                5,
                changeCounts[sourceIndex]
            );
        }

        [Test]
        public void DisabledHorizontalFlowProducesNoHorizontalChanges()
        {
            FluidSimulationSettings settings =
                FluidSimulationSettings.Default;

            settings.AllowHorizontalFlow = false;

            int sourceIndex =
                VoxelIndex.ToIndex(
                    8,
                    8,
                    8
                );

            voxels[sourceIndex] =
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                );

            RunJob(settings);

            AssertChangeDoesNotExist(
                sourceIndex,
                9,
                8,
                8
            );

            AssertChangeDoesNotExist(
                sourceIndex,
                7,
                8,
                8
            );

            AssertChangeDoesNotExist(
                sourceIndex,
                8,
                8,
                9
            );

            AssertChangeDoesNotExist(
                sourceIndex,
                8,
                8,
                7
            );

            AssertChangeExists(
                sourceIndex,
                8,
                7,
                8
            );
        }

        [Test]
        public void DisabledVerticalFlowProducesNoVerticalChange()
        {
            FluidSimulationSettings settings =
                FluidSimulationSettings.Default;

            settings.AllowVerticalFlow = false;

            int sourceIndex =
                VoxelIndex.ToIndex(
                    8,
                    8,
                    8
                );

            voxels[sourceIndex] =
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                );

            RunJob(settings);

            AssertChangeDoesNotExist(
                sourceIndex,
                8,
                7,
                8
            );

            AssertChangeExists(
                sourceIndex,
                9,
                8,
                8
            );
        }

        private void RunJob()
        {
            RunJob(
                FluidSimulationSettings.Default
            );
        }

        private void RunJob(
            FluidSimulationSettings settings)
        {
            FluidSimulationJob job =
                new FluidSimulationJob
                {
                    Voxels = voxels,

                    FluidsByBlockId =
                        fluidsByBlockId,

                    Settings = settings,

                    ChunkCoordinate =
                        new ChunkCoordinate(
                            0,
                            0,
                            0
                        ),

                    Changes = changes,

                    ChangeCounts =
                        changeCounts
                };

            for (int index = 0;
                 index < VoxelConstants.VoxelsPerChunk;
                 index++)
            {
                job.Execute(index);
            }
        }

        private FluidChange FindChange(
            int sourceIndex,
            int x,
            int y,
            int z)
        {
            int count =
                changeCounts[sourceIndex];

            for (int i = 0;
                 i < count;
                 i++)
            {
                int index =
                    sourceIndex *
                    FluidChangeBuffer.MaxChangesPerVoxel +
                    i;

                FluidChange change =
                    changes[index];

                if (change.X == x &&
                    change.Y == y &&
                    change.Z == z)
                {
                    return change;
                }
            }

            Assert.Fail(
                $"No se encontró un cambio hacia " +
                $"({x}, {y}, {z})."
            );

            return default;
        }

        private void AssertChangeExists(
            int sourceIndex,
            int x,
            int y,
            int z)
        {
            FindChange(
                sourceIndex,
                x,
                y,
                z
            );
        }

        private void AssertChangeDoesNotExist(
            int sourceIndex,
            int x,
            int y,
            int z)
        {
            int count =
                changeCounts[sourceIndex];

            for (int i = 0;
                 i < count;
                 i++)
            {
                int index =
                    sourceIndex *
                    FluidChangeBuffer.MaxChangesPerVoxel +
                    i;

                FluidChange change =
                    changes[index];

                if (change.X == x &&
                    change.Y == y &&
                    change.Z == z)
                {
                    Assert.Fail(
                        $"Se encontró un cambio inesperado " +
                        $"hacia ({x}, {y}, {z})."
                    );
                }
            }
        }

        [Test]
        public void BoundaryFluidCreatesChangeForNeighborChunk()
        {
            int sourceIndex =
                VoxelIndex.ToIndex(
                    15,
                    8,
                    8
                );

            voxels[sourceIndex] =
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                );

            FluidSimulationSettings settings =
                FluidSimulationSettings.Default;

            settings.AllowVerticalFlow = false;

            RunJob(settings);

            bool found = false;

            int count =
                changeCounts[sourceIndex];

            for (int i = 0;
                 i < count;
                 i++)
            {
                int index =
                    sourceIndex *
                    FluidChangeBuffer.MaxChangesPerVoxel +
                    i;

                FluidChange change =
                    changes[index];

                if (change.TargetChunk ==
                        new ChunkCoordinate(
                            1,
                            0,
                            0
                        ) &&
                    change.X == 0 &&
                    change.Y == 8 &&
                    change.Z == 8)
                {
                    found = true;

                    Assert.AreEqual(
                        FluidType.Water,
                        change.State.Type
                    );

                    Assert.AreEqual(
                        14,
                        change.State.Level
                    );

                    break;
                }
            }

            Assert.IsTrue(
                found,
                "No se generó el cambio hacia el chunk vecino."
            );
        }
    }
}
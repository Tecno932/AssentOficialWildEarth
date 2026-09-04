using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace WildEarth.Voxel.Tests
{
    public sealed class FluidSchedulerTests
    {
        private const ushort WaterBlockId = 100;
        private const ushort LavaBlockId = 101;

        private ChunkDataPool dataPool;
        private ChunkBiomeDataPool biomeDataPool;
        private ChunkStorage chunkStorage;

        private FluidDefinition waterDefinition;
        private FluidDefinition lavaDefinition;

        private FluidRegistry registry;
        private FluidRuntimeDatabase fluidDatabase;
        private FluidUpdateSystem updateSystem;
        private FluidScheduler scheduler;

        private ChunkCoordinate chunkCoordinate;
        private ChunkCoordinate neighborCoordinate;

        [SetUp]
        public void SetUp()
        {
            dataPool =
                new ChunkDataPool(
                    Allocator.Persistent,
                    4,
                    16
                );

            biomeDataPool =
                new ChunkBiomeDataPool(
                    Allocator.Persistent,
                    4,
                    16
                );

            chunkStorage =
                new ChunkStorage(
                    dataPool,
                    biomeDataPool
                );

            chunkCoordinate =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            neighborCoordinate =
                new ChunkCoordinate(
                    1,
                    0,
                    0
                );

            chunkStorage.Create(
                chunkCoordinate
            );

            chunkStorage.Create(
                neighborCoordinate
            );

            waterDefinition =
                CreateFluidDefinition(
                    FluidType.Water,
                    "Water",
                    WaterBlockId,
                    false
                );

            lavaDefinition =
                CreateFluidDefinition(
                    FluidType.Lava,
                    "Lava",
                    LavaBlockId,
                    true
                );

            registry =
                new FluidRegistry(
                    new[]
                    {
                        waterDefinition,
                        lavaDefinition
                    }
                );

            fluidDatabase =
                new FluidRuntimeDatabase(
                    registry,
                    Allocator.Persistent
                );

            updateSystem =
                new FluidUpdateSystem(
                    chunkStorage,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            scheduler =
                new FluidScheduler(
                    updateSystem,
                    FluidSimulationSettings.Default
                );
        }

        [TearDown]
        public void TearDown()
        {
            if (scheduler != null)
                scheduler.Clear();

            if (fluidDatabase != null)
                fluidDatabase.Dispose();

            if (chunkStorage != null)
                chunkStorage.Dispose();

            if (waterDefinition != null)
                Object.DestroyImmediate(
                    waterDefinition
                );

            if (lavaDefinition != null)
                Object.DestroyImmediate(
                    lavaDefinition
                );
        }

        [Test]
        public void NewSchedulerHasNoPendingUpdates()
        {
            Assert.AreEqual(
                0,
                scheduler.PendingCount
            );

            Assert.IsFalse(
                scheduler.HasPendingUpdates
            );
        }

        [Test]
        public void NewSchedulerHasNoActiveChunks()
        {
            Assert.AreEqual(
                0,
                scheduler.ActiveChunkCount
            );
        }

        [Test]
        public void EnqueueAddsUpdate()
        {
            FluidPendingUpdate update =
                CreateWaterUpdate();

            bool added =
                scheduler.Enqueue(
                    update
                );

            Assert.IsTrue(added);

            Assert.AreEqual(
                1,
                scheduler.PendingCount
            );

            Assert.IsTrue(
                scheduler.HasPendingUpdates
            );
        }

        [Test]
        public void EnqueueActivatesChunk()
        {
            scheduler.Enqueue(
                CreateWaterUpdate()
            );

            Assert.IsTrue(
                scheduler.IsChunkActive(
                    chunkCoordinate
                )
            );
        }

        [Test]
        public void EnqueueRejectsInvalidUpdate()
        {
            FluidPendingUpdate update =
                new FluidPendingUpdate(
                    chunkCoordinate,
                    -1,
                    0,
                    0,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            bool added =
                scheduler.Enqueue(
                    update
                );

            Assert.IsFalse(added);

            Assert.AreEqual(
                0,
                scheduler.PendingCount
            );
        }

        [Test]
        public void EnqueueRejectsEmptyFluid()
        {
            FluidPendingUpdate update =
                new FluidPendingUpdate(
                    chunkCoordinate,
                    0,
                    0,
                    0,
                    FluidState.Empty
                );

            bool added =
                scheduler.Enqueue(
                    update
                );

            Assert.IsFalse(added);

            Assert.AreEqual(
                0,
                scheduler.PendingCount
            );
        }

        [Test]
        public void EnqueueRejectsUpdateBeyondPropagationDistance()
        {
            FluidSimulationSettings settings =
                FluidSimulationSettings.Default;

            settings.MaxPropagationDistance = 4;

            FluidScheduler limitedScheduler =
                new FluidScheduler(
                    updateSystem,
                    settings
                );

            try
            {
                FluidPendingUpdate update =
                    CreateWaterUpdate(
                        distance: 5
                    );

                bool added =
                    limitedScheduler.Enqueue(
                        update
                    );

                Assert.IsFalse(added);

                Assert.AreEqual(
                    0,
                    limitedScheduler.PendingCount
                );
            }
            finally
            {
                limitedScheduler.Clear();
            }
        }

        [Test]
        public void EnqueueAcceptsUpdateAtPropagationLimit()
        {
            FluidSimulationSettings settings =
                FluidSimulationSettings.Default;

            settings.MaxPropagationDistance = 4;

            FluidScheduler limitedScheduler =
                new FluidScheduler(
                    updateSystem,
                    settings
                );

            try
            {
                FluidPendingUpdate update =
                    CreateWaterUpdate(
                        distance: 4
                    );

                bool added =
                    limitedScheduler.Enqueue(
                        update
                    );

                Assert.IsTrue(added);

                Assert.AreEqual(
                    1,
                    limitedScheduler.PendingCount
                );
            }
            finally
            {
                limitedScheduler.Clear();
            }
        }

        [Test]
        public void DuplicateUpdateIsRejected()
        {
            FluidPendingUpdate update =
                CreateWaterUpdate();

            bool first =
                scheduler.Enqueue(
                    update
                );

            bool second =
                scheduler.Enqueue(
                    update
                );

            Assert.IsTrue(first);
            Assert.IsFalse(second);

            Assert.AreEqual(
                1,
                scheduler.PendingCount
            );
        }

        [Test]
        public void SameVoxelDifferentFluidLevelIsDeduplicated()
        {
            FluidPendingUpdate first =
                CreateWaterUpdate(
                    level: 15
                );

            FluidPendingUpdate second =
                CreateWaterUpdate(
                    level: 10
                );

            Assert.IsTrue(
                scheduler.Enqueue(first)
            );

            Assert.IsFalse(
                scheduler.Enqueue(second)
            );

            Assert.AreEqual(
                1,
                scheduler.PendingCount
            );
        }

        [Test]
        public void DifferentVoxelsCanBeQueued()
        {
            FluidPendingUpdate first =
                CreateWaterUpdate(
                    x: 1
                );

            FluidPendingUpdate second =
                CreateWaterUpdate(
                    x: 2
                );

            Assert.IsTrue(
                scheduler.Enqueue(first)
            );

            Assert.IsTrue(
                scheduler.Enqueue(second)
            );

            Assert.AreEqual(
                2,
                scheduler.PendingCount
            );
        }

        [Test]
        public void DifferentChunksCanBeQueued()
        {
            FluidPendingUpdate first =
                CreateWaterUpdate(
                    chunk: chunkCoordinate
                );

            FluidPendingUpdate second =
                CreateWaterUpdate(
                    chunk: neighborCoordinate
                );

            Assert.IsTrue(
                scheduler.Enqueue(first)
            );

            Assert.IsTrue(
                scheduler.Enqueue(second)
            );

            Assert.AreEqual(
                2,
                scheduler.PendingCount
            );

            Assert.AreEqual(
                2,
                scheduler.ActiveChunkCount
            );
        }

        [Test]
        public void RemoveNextReturnsQueuedUpdate()
        {
            FluidPendingUpdate expected =
                CreateWaterUpdate();

            scheduler.Enqueue(
                expected
            );

            bool removed =
                scheduler.RemoveNext(
                    out FluidPendingUpdate actual
                );

            Assert.IsTrue(removed);

            Assert.AreEqual(
                expected,
                actual
            );
        }

        [Test]
        public void RemoveNextRemovesPendingKey()
        {
            FluidPendingUpdate update =
                CreateWaterUpdate();

            scheduler.Enqueue(
                update
            );

            scheduler.RemoveNext(
                out _
            );

            Assert.IsFalse(
                scheduler.HasPendingUpdate(
                    update
                )
            );
        }

        [Test]
        public void RemoveNextReturnsFalseWhenEmpty()
        {
            bool removed =
                scheduler.RemoveNext(
                    out FluidPendingUpdate update
                );

            Assert.IsFalse(removed);

            Assert.AreEqual(
                default(FluidPendingUpdate),
                update
            );
        }

        [Test]
        public void ProcessTickProcessesPendingUpdate()
        {
            scheduler.Enqueue(
                CreateWaterUpdate()
            );

            int processed =
                scheduler.ProcessTick();

            Assert.AreEqual(
                1,
                processed
            );

            Assert.AreEqual(
                0,
                scheduler.PendingCount
            );
        }

        [Test]
        public void ProcessTickAppliesFluidToChunk()
        {
            FluidPendingUpdate update =
                CreateWaterUpdate(
                    x: 3,
                    y: 4,
                    z: 5,
                    level: 12
                );

            scheduler.Enqueue(
                update
            );

            scheduler.ProcessTick();

            Chunk chunk;

            Assert.IsTrue(
                chunkStorage.TryGet(
                    chunkCoordinate,
                    out chunk
                )
            );

            Voxel voxel =
                ChunkDataAccess.GetVoxel(
                    chunk.Data,
                    3,
                    4,
                    5
                );

            Assert.AreEqual(
                WaterBlockId,
                voxel.BlockId
            );

            Assert.AreEqual(
                12,
                voxel.State
            );
        }

        [Test]
        public void ProcessTickRemovesProcessedUpdate()
        {
            FluidPendingUpdate update =
                CreateWaterUpdate();

            scheduler.Enqueue(
                update
            );

            scheduler.ProcessTick();

            Assert.IsFalse(
                scheduler.HasPendingUpdate(
                    update
                )
            );

            Assert.AreEqual(
                0,
                scheduler.PendingCount
            );
        }

        [Test]
        public void ProcessTickRespectsUpdateBudget()
        {
            FluidSimulationSettings settings =
                FluidSimulationSettings.Default;

            settings.MaxUpdatesPerTick = 2;

            FluidScheduler limitedScheduler =
                new FluidScheduler(
                    updateSystem,
                    settings
                );

            try
            {
                limitedScheduler.Enqueue(
                    CreateWaterUpdate(x: 1)
                );

                limitedScheduler.Enqueue(
                    CreateWaterUpdate(x: 2)
                );

                limitedScheduler.Enqueue(
                    CreateWaterUpdate(x: 3)
                );

                int processed =
                    limitedScheduler.ProcessTick();

                Assert.AreEqual(
                    2,
                    processed
                );

                Assert.AreEqual(
                    1,
                    limitedScheduler.PendingCount
                );
            }
            finally
            {
                limitedScheduler.Clear();
            }
        }

        [Test]
        public void ProcessTickProcessesRemainingUpdatesOnNextTick()
        {
            FluidSimulationSettings settings =
                FluidSimulationSettings.Default;

            settings.MaxUpdatesPerTick = 2;

            FluidScheduler limitedScheduler =
                new FluidScheduler(
                    updateSystem,
                    settings
                );

            try
            {
                limitedScheduler.Enqueue(
                    CreateWaterUpdate(x: 1)
                );

                limitedScheduler.Enqueue(
                    CreateWaterUpdate(x: 2)
                );

                limitedScheduler.Enqueue(
                    CreateWaterUpdate(x: 3)
                );

                Assert.AreEqual(
                    2,
                    limitedScheduler.ProcessTick()
                );

                Assert.AreEqual(
                    1,
                    limitedScheduler.PendingCount
                );

                Assert.AreEqual(
                    1,
                    limitedScheduler.ProcessTick()
                );

                Assert.AreEqual(
                    0,
                    limitedScheduler.PendingCount
                );
            }
            finally
            {
                limitedScheduler.Clear();
            }
        }

        [Test]
        public void ProcessTickRejectsSolidTargetWithoutCrashing()
        {
            Chunk chunk;

            Assert.IsTrue(
                chunkStorage.TryGet(
                    chunkCoordinate,
                    out chunk
                )
            );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                2,
                2,
                2,
                new Voxel(
                    1
                )
            );

            scheduler.Enqueue(
                CreateWaterUpdate(
                    x: 2,
                    y: 2,
                    z: 2
                )
            );

            int processed =
                scheduler.ProcessTick();

            Assert.AreEqual(
                1,
                processed
            );

            Voxel voxel =
                ChunkDataAccess.GetVoxel(
                    chunk.Data,
                    2,
                    2,
                    2
                );

            Assert.AreEqual(
                1,
                voxel.BlockId
            );
        }

        [Test]
        public void ProcessTickCanApplyToNeighborChunk()
        {
            FluidPendingUpdate update =
                CreateWaterUpdate(
                    chunk: neighborCoordinate,
                    x: 4,
                    y: 5,
                    z: 6,
                    level: 9
                );

            scheduler.Enqueue(
                update
            );

            int processed =
                scheduler.ProcessTick();

            Assert.AreEqual(
                1,
                processed
            );

            Chunk neighbor;

            Assert.IsTrue(
                chunkStorage.TryGet(
                    neighborCoordinate,
                    out neighbor
                )
            );

            Voxel voxel =
                ChunkDataAccess.GetVoxel(
                    neighbor.Data,
                    4,
                    5,
                    6
                );

            Assert.AreEqual(
                WaterBlockId,
                voxel.BlockId
            );

            Assert.AreEqual(
                9,
                voxel.State
            );
        }

        [Test]
        public void ActivateChunkMarksChunkActive()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    5,
                    0,
                    5
                );

            scheduler.ActivateChunk(
                coordinate
            );

            Assert.IsTrue(
                scheduler.IsChunkActive(
                    coordinate
                )
            );
        }

        [Test]
        public void ActivateChunkIsIdempotent()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    5,
                    0,
                    5
                );

            scheduler.ActivateChunk(
                coordinate
            );

            scheduler.ActivateChunk(
                coordinate
            );

            Assert.AreEqual(
                1,
                scheduler.ActiveChunkCount
            );
        }

        [Test]
        public void ClearRemovesPendingUpdates()
        {
            scheduler.Enqueue(
                CreateWaterUpdate()
            );

            scheduler.Clear();

            Assert.AreEqual(
                0,
                scheduler.PendingCount
            );

            Assert.IsFalse(
                scheduler.HasPendingUpdates
            );
        }

        [Test]
        public void ClearRemovesActiveChunks()
        {
            scheduler.Enqueue(
                CreateWaterUpdate()
            );

            scheduler.ActivateChunk(
                neighborCoordinate
            );

            scheduler.Clear();

            Assert.AreEqual(
                0,
                scheduler.ActiveChunkCount
            );
        }

        [Test]
        public void ClearAllowsSameUpdateToBeQueuedAgain()
        {
            FluidPendingUpdate update =
                CreateWaterUpdate();

            Assert.IsTrue(
                scheduler.Enqueue(update)
            );

            scheduler.Clear();

            Assert.IsTrue(
                scheduler.Enqueue(update)
            );

            Assert.AreEqual(
                1,
                scheduler.PendingCount
            );
        }

        [Test]
        public void ClearChunkRemovesOnlyThatChunksUpdates()
        {
            FluidPendingUpdate first =
                CreateWaterUpdate(
                    chunk: chunkCoordinate,
                    x: 1
                );

            FluidPendingUpdate second =
                CreateWaterUpdate(
                    chunk: neighborCoordinate,
                    x: 2
                );

            scheduler.Enqueue(first);
            scheduler.Enqueue(second);

            scheduler.ClearChunk(
                chunkCoordinate
            );

            Assert.AreEqual(
                1,
                scheduler.PendingCount
            );

            Assert.IsFalse(
                scheduler.HasPendingUpdate(
                    first
                )
            );

            Assert.IsTrue(
                scheduler.HasPendingUpdate(
                    second
                )
            );
        }

        [Test]
        public void ClearChunkDeactivatesChunk()
        {
            scheduler.Enqueue(
                CreateWaterUpdate(
                    chunk: chunkCoordinate
                )
            );

            scheduler.ClearChunk(
                chunkCoordinate
            );

            Assert.IsFalse(
                scheduler.IsChunkActive(
                    chunkCoordinate
                )
            );
        }

        [Test]
        public void ClearChunkDoesNotAffectOtherChunk()
        {
            scheduler.Enqueue(
                CreateWaterUpdate(
                    chunk: chunkCoordinate
                )
            );

            scheduler.Enqueue(
                CreateWaterUpdate(
                    chunk: neighborCoordinate,
                    x: 2
                )
            );

            scheduler.ClearChunk(
                chunkCoordinate
            );

            Assert.IsTrue(
                scheduler.IsChunkActive(
                    neighborCoordinate
                )
            );
        }

        [Test]
        public void EnqueueRangeAddsValidUpdates()
        {
            List<FluidPendingUpdate> updates =
                new List<FluidPendingUpdate>
                {
                    CreateWaterUpdate(x: 1),
                    CreateWaterUpdate(x: 2),
                    CreateWaterUpdate(x: 3)
                };

            int added =
                scheduler.EnqueueRange(
                    updates
                );

            Assert.AreEqual(
                3,
                added
            );

            Assert.AreEqual(
                3,
                scheduler.PendingCount
            );
        }

        [Test]
        public void EnqueueRangeDeduplicatesUpdates()
        {
            FluidPendingUpdate first =
                CreateWaterUpdate();

            FluidPendingUpdate duplicate =
                CreateWaterUpdate();

            FluidPendingUpdate second =
                CreateWaterUpdate(
                    x: 2
                );

            int added =
                scheduler.EnqueueRange(
                    new[]
                    {
                        first,
                        duplicate,
                        second
                    }
                );

            Assert.AreEqual(
                2,
                added
            );

            Assert.AreEqual(
                2,
                scheduler.PendingCount
            );
        }

        [Test]
        public void EnqueueRangeRejectsInvalidUpdates()
        {
            FluidPendingUpdate valid =
                CreateWaterUpdate();

            FluidPendingUpdate invalid =
                new FluidPendingUpdate(
                    chunkCoordinate,
                    -1,
                    0,
                    0,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            int added =
                scheduler.EnqueueRange(
                    new[]
                    {
                        valid,
                        invalid
                    }
                );

            Assert.AreEqual(
                1,
                added
            );

            Assert.AreEqual(
                1,
                scheduler.PendingCount
            );
        }

        [Test]
        public void AdvanceDoesNotProcessBeforeTickInterval()
        {
            FluidSimulationSettings settings =
                FluidSimulationSettings.Default;

            settings.TicksPerSecond = 10;

            FluidScheduler timedScheduler =
                new FluidScheduler(
                    updateSystem,
                    settings
                );

            try
            {
                timedScheduler.Enqueue(
                    CreateWaterUpdate()
                );

                int processed =
                    timedScheduler.Advance(
                        0.05f
                    );

                Assert.AreEqual(
                    0,
                    processed
                );

                Assert.AreEqual(
                    1,
                    timedScheduler.PendingCount
                );
            }
            finally
            {
                timedScheduler.Clear();
            }
        }

        [Test]
        public void AdvanceProcessesAfterTickInterval()
        {
            FluidSimulationSettings settings =
                FluidSimulationSettings.Default;

            settings.TicksPerSecond = 10;

            FluidScheduler timedScheduler =
                new FluidScheduler(
                    updateSystem,
                    settings
                );

            try
            {
                timedScheduler.Enqueue(
                    CreateWaterUpdate()
                );

                int processed =
                    timedScheduler.Advance(
                        0.1f
                    );

                Assert.AreEqual(
                    1,
                    processed
                );

                Assert.AreEqual(
                    0,
                    timedScheduler.PendingCount
                );
            }
            finally
            {
                timedScheduler.Clear();
            }
        }

        [Test]
        public void AdvanceAccumulatesPartialTime()
        {
            FluidSimulationSettings settings =
                FluidSimulationSettings.Default;

            settings.TicksPerSecond = 10;

            FluidScheduler timedScheduler =
                new FluidScheduler(
                    updateSystem,
                    settings
                );

            try
            {
                timedScheduler.Enqueue(
                    CreateWaterUpdate()
                );

                Assert.AreEqual(
                    0,
                    timedScheduler.Advance(
                        0.05f
                    )
                );

                Assert.AreEqual(
                    1,
                    timedScheduler.Advance(
                        0.05f
                    )
                );

                Assert.AreEqual(
                    0,
                    timedScheduler.PendingCount
                );
            }
            finally
            {
                timedScheduler.Clear();
            }
        }

        [Test]
        public void AdvanceRejectsNegativeDeltaTime()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () =>
                    scheduler.Advance(
                        -0.1f
                    )
            );
        }

        [Test]
        public void AdvanceProcessesMultipleTicksWhenEnoughTimeAccumulated()
        {
            FluidSimulationSettings settings =
                FluidSimulationSettings.Default;

            settings.TicksPerSecond = 10;
            settings.MaxUpdatesPerTick = 1;

            FluidScheduler timedScheduler =
                new FluidScheduler(
                    updateSystem,
                    settings
                );

            try
            {
                timedScheduler.Enqueue(
                    CreateWaterUpdate(
                        x: 1
                    )
                );

                timedScheduler.Enqueue(
                    CreateWaterUpdate(
                        x: 2
                    )
                );

                int processed =
                    timedScheduler.Advance(
                        0.2f
                    );

                Assert.AreEqual(
                    2,
                    processed
                );

                Assert.AreEqual(
                    0,
                    timedScheduler.PendingCount
                );
            }
            finally
            {
                timedScheduler.Clear();
            }
        }

        [Test]
        public void SchedulerPreservesQueueOrder()
        {
            FluidPendingUpdate first =
                CreateWaterUpdate(
                    x: 1
                );

            FluidPendingUpdate second =
                CreateWaterUpdate(
                    x: 2
                );

            FluidPendingUpdate third =
                CreateWaterUpdate(
                    x: 3
                );

            scheduler.Enqueue(first);
            scheduler.Enqueue(second);
            scheduler.Enqueue(third);

            scheduler.RemoveNext(
                out FluidPendingUpdate result1
            );

            scheduler.RemoveNext(
                out FluidPendingUpdate result2
            );

            scheduler.RemoveNext(
                out FluidPendingUpdate result3
            );

            Assert.AreEqual(
                first,
                result1
            );

            Assert.AreEqual(
                second,
                result2
            );

            Assert.AreEqual(
                third,
                result3
            );
        }

        [Test]
        public void ProcessingRejectedUpdateStillConsumesBudget()
        {
            FluidSimulationSettings settings =
                FluidSimulationSettings.Default;

            settings.MaxUpdatesPerTick = 1;

            FluidScheduler limitedScheduler =
                new FluidScheduler(
                    updateSystem,
                    settings
                );

            try
            {
                Chunk chunk;

                Assert.IsTrue(
                    chunkStorage.TryGet(
                        chunkCoordinate,
                        out chunk
                    )
                );

                ChunkDataAccess.SetVoxel(
                    chunk.Data,
                    1,
                    1,
                    1,
                    new Voxel(1)
                );

                limitedScheduler.Enqueue(
                    CreateWaterUpdate(
                        x: 1,
                        y: 1,
                        z: 1
                    )
                );

                limitedScheduler.Enqueue(
                    CreateWaterUpdate(
                        x: 2,
                        y: 2,
                        z: 2
                    )
                );

                int processed =
                    limitedScheduler.ProcessTick();

                Assert.AreEqual(
                    1,
                    processed
                );

                Assert.AreEqual(
                    1,
                    limitedScheduler.PendingCount
                );
            }
            finally
            {
                limitedScheduler.Clear();
            }
        }

        private FluidPendingUpdate CreateWaterUpdate(
            ChunkCoordinate? chunk = null,
            int x = 0,
            int y = 0,
            int z = 0,
            byte level = 15,
            int distance = 0)
        {
            return new FluidPendingUpdate(
                chunk ?? chunkCoordinate,
                x,
                y,
                z,
                new FluidState(
                    FluidType.Water,
                    level
                ),
                distance
            );
        }

        private static FluidDefinition CreateFluidDefinition(
            FluidType type,
            string name,
            ushort blockId,
            bool isLava)
        {
            FluidDefinition definition =
                ScriptableObject.CreateInstance<FluidDefinition>();

            SetPrivateField(
                definition,
                "type",
                type
            );

            SetPrivateField(
                definition,
                "fluidName",
                name
            );

            SetPrivateField(
                definition,
                "blockId",
                blockId
            );

            SetPrivateField(
                definition,
                "maxLevel",
                FluidState.MaxLevel
            );

            SetPrivateField(
                definition,
                "horizontalFlowDecay",
                (byte)1
            );

            SetPrivateField(
                definition,
                "verticalFlowDecay",
                (byte)0
            );

            SetPrivateField(
                definition,
                "isLava",
                isLava
            );

            return definition;
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            var field =
                target.GetType().GetField(
                    fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic
                );

            Assert.IsNotNull(
                field,
                $"No se encontró el campo privado '{fieldName}'."
            );

            field.SetValue(
                target,
                value
            );
        }
    }
}
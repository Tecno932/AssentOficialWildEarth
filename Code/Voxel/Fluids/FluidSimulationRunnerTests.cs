using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace WildEarth.Voxel.Tests
{
    public sealed class FluidSimulationRunnerTests
    {
        private const ushort WaterBlockId = 100;

        private FluidDefinition waterDefinition;

        private FluidRegistry registry;
        private FluidRuntimeDatabase database;
        private FluidUpdateSystem updateSystem;
        private FluidScheduler scheduler;
        private FluidSimulationRunner runner;
        private ChunkData chunkData;

        private ChunkStorage storage;
        private ChunkDataPool dataPool;
        private ChunkBiomeDataPool biomePool;

        [SetUp]
        public void SetUp()
        {
            waterDefinition =
                CreateFluidDefinition(
                    FluidType.Water,
                    WaterBlockId,
                    "Test Water",
                    false
                );

            registry =
                new FluidRegistry(
                    new[]
                    {
                        waterDefinition
                    }
                );

            database =
                new FluidRuntimeDatabase(
                    registry,
                    Allocator.Persistent
                );

            dataPool =
                new ChunkDataPool(
                    Allocator.Persistent,
                    1,
                    4
                );

            biomePool =
                new ChunkBiomeDataPool(
                    Allocator.Persistent,
                    1,
                    4
                );

            storage =
                new ChunkStorage(
                    dataPool,
                    biomePool
                );

            updateSystem =
                new FluidUpdateSystem(
                    storage,
                    database,
                    FluidSimulationSettings.Default
                );

            scheduler =
                new FluidScheduler(
                    updateSystem,
                    FluidSimulationSettings.Default
                );

            runner =
                new FluidSimulationRunner(
                    database,
                    FluidSimulationSettings.Default
                );

            chunkData =
                new ChunkData(
                    Allocator.Persistent
                );
        }

        [TearDown]
        public void TearDown()
        {
            if (runner != null)
                runner.Dispose();

            if (chunkData != null)
                chunkData.Dispose();

            if (storage != null)
                storage.Clear();

            if (database != null)
                database.Dispose();

            if (waterDefinition != null)
                Object.DestroyImmediate(
                    waterDefinition
                );
        }

        [Test]
        public void RunnerStartsNotScheduled()
        {
            Assert.IsFalse(
                runner.IsScheduled
            );
        }

        [Test]
        public void RunnerStartsCompleted()
        {
            Assert.IsTrue(
                runner.IsCompleted
            );
        }

        [Test]
        public void ScheduleStartsJob()
        {
            SetWater(
                8,
                8,
                8,
                15
            );

            runner.Schedule(
                chunkData,
                new ChunkCoordinate(
                    0,
                    0,
                    0
                )
            );

            Assert.IsTrue(
                runner.IsScheduled
            );
        }

        [Test]
        public void CompleteFinishesJob()
        {
            SetWater(
                8,
                8,
                8,
                15
            );

            runner.Schedule(
                chunkData,
                new ChunkCoordinate(
                    0,
                    0,
                    0
                )
            );

            runner.Complete();

            Assert.IsTrue(
                runner.IsCompleted
            );
        }

        [Test]
        public void ResultIsAvailableAfterSchedule()
        {
            SetWater(
                8,
                8,
                8,
                15
            );

            runner.Schedule(
                chunkData,
                new ChunkCoordinate(
                    0,
                    0,
                    0
                )
            );

            runner.Complete();

            Assert.IsNotNull(
                runner.Result
            );

            Assert.IsTrue(
                runner.Result.IsCreated
            );
        }

        [Test]
        public void WaterProducesPendingUpdates()
        {
            SetWater(
                8,
                8,
                8,
                15
            );

            runner.Schedule(
                chunkData,
                new ChunkCoordinate(
                    0,
                    0,
                    0
                )
            );

            runner.Complete();

            int added =
                runner.EnqueueResults(
                    scheduler
                );

            Assert.Greater(
                added,
                0
            );

            Assert.Greater(
                scheduler.PendingCount,
                0
            );
        }

        [Test]
        public void EmptyChunkProducesNoUpdates()
        {
            runner.Schedule(
                chunkData,
                new ChunkCoordinate(
                    0,
                    0,
                    0
                )
            );

            runner.Complete();

            int added =
                runner.EnqueueResults(
                    scheduler
                );

            Assert.AreEqual(
                0,
                added
            );

            Assert.AreEqual(
                0,
                scheduler.PendingCount
            );
        }

        [Test]
        public void ResultContainsWaterPropagation()
        {
            SetWater(
                8,
                8,
                8,
                15
            );

            runner.Schedule(
                chunkData,
                new ChunkCoordinate(
                    0,
                    0,
                    0
                )
            );

            runner.Complete();

            bool foundWater = false;

            for (
                int voxelIndex = 0;
                voxelIndex < runner.Result.VoxelCount;
                voxelIndex++)
            {
                int count =
                    runner.Result.GetChangeCount(
                        voxelIndex
                    );

                for (
                    int changeIndex = 0;
                    changeIndex < count;
                    changeIndex++)
                {
                    FluidChange change =
                        runner.Result.GetChange(
                            voxelIndex,
                            changeIndex
                        );

                    if (
                        change.State.Type ==
                        FluidType.Water
                    )
                    {
                        foundWater = true;
                        break;
                    }
                }

                if (foundWater)
                    break;
            }

            Assert.IsTrue(
                foundWater
            );
        }

        [Test]
        public void EnqueueResultsPreservesChunkCoordinate()
        {
            SetWater(
                15,
                8,
                8,
                15
            );

            ChunkCoordinate sourceChunk =
                new ChunkCoordinate(
                    0,
                    0,
                    0
                );

            runner.Schedule(
                chunkData,
                sourceChunk
            );

            runner.Complete();

            runner.EnqueueResults(
                scheduler
            );

            bool foundNeighbor = false;

            while (
                scheduler.RemoveNext(
                    out FluidPendingUpdate update
                ))
            {
                if (
                    update.Chunk ==
                    new ChunkCoordinate(
                        1,
                        0,
                        0
                    )
                )
                {
                    foundNeighbor = true;
                    break;
                }
            }

            Assert.IsTrue(
                foundNeighbor
            );
        }

        [Test]
        public void CannotScheduleTwiceWithoutCompletingPreviousJob()
        {
            runner.Schedule(
                chunkData,
                new ChunkCoordinate(
                    0,
                    0,
                    0
                )
            );

            Assert.Throws<System.InvalidOperationException>(
                () =>
                    runner.Schedule(
                        chunkData,
                        new ChunkCoordinate(
                            0,
                            0,
                            0
                        )
                    )
            );
        }

        [Test]
        public void DisposeMakesRunnerUnavailable()
        {
            runner.Dispose();

            Assert.Throws<System.ObjectDisposedException>(
                () =>
                    runner.Schedule(
                        chunkData,
                        new ChunkCoordinate(
                            0,
                            0,
                            0
                        )
                    )
            );
        }

        [Test]
        public void DisposeCanBeCalledTwice()
        {
            runner.Dispose();
            runner.Dispose();

            Assert.Pass();
        }

        private void SetWater(
            int x,
            int y,
            int z,
            byte level)
        {
            NativeArray<Voxel> voxels =
                chunkData.Voxels;

            voxels[
                VoxelIndex.ToIndex(
                    x,
                    y,
                    z
                )
            ] =
                new Voxel(
                    WaterBlockId,
                    0,
                    level
                );
        }

        private static FluidDefinition
            CreateFluidDefinition(
                FluidType type,
                ushort blockId,
                string fluidName,
                bool isLava)
        {
            FluidDefinition definition =
                ScriptableObject.CreateInstance<
                    FluidDefinition
                >();

            SerializedObject serialized =
                new SerializedObject(
                    definition
                );

            serialized.FindProperty(
                "type"
            ).enumValueIndex =
                (int)type;

            serialized.FindProperty(
                "fluidName"
            ).stringValue =
                fluidName;

            serialized.FindProperty(
                "blockId"
            ).intValue =
                blockId;

            serialized.FindProperty(
                "maxLevel"
            ).intValue =
                FluidState.MaxLevel;

            serialized.FindProperty(
                "horizontalFlowDecay"
            ).intValue =
                1;

            serialized.FindProperty(
                "verticalFlowDecay"
            ).intValue =
                0;

            serialized.FindProperty(
                "isLava"
            ).boolValue =
                isLava;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            return definition;
        }
    }
}
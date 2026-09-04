using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace WildEarth.Voxel.Tests
{
    public sealed class FluidSimulationCoordinatorTests
    {
        private FluidDefinition waterDefinition;

        private FluidRuntimeDatabase fluidDatabase;

        private ChunkDataPool dataPool;
        private ChunkBiomeDataPool biomeDataPool;

        private ChunkStorage storage;

        private FluidUpdateSystem updateSystem;
        private FluidScheduler scheduler;

        private FluidSimulationCoordinator coordinator;

        private Chunk chunk;

        private const ushort WaterBlockId = 100;

        [SetUp]
        public void SetUp()
        {
            waterDefinition =
                CreateFluidDefinition(
                    FluidType.Water,
                    "Water",
                    WaterBlockId,
                    15,
                    1,
                    0,
                    false
                );

            FluidRegistry registry =
                new FluidRegistry(
                    new[]
                    {
                        waterDefinition
                    }
                );

            fluidDatabase =
                new FluidRuntimeDatabase(
                    registry,
                    Allocator.Persistent
                );

            dataPool =
                new ChunkDataPool(
                    Allocator.Persistent,
                    1,
                    8
                );

            biomeDataPool =
                new ChunkBiomeDataPool(
                    Allocator.Persistent,
                    1,
                    8
                );

            storage =
                new ChunkStorage(
                    dataPool,
                    biomeDataPool
                );

            chunk =
                storage.Create(
                    new ChunkCoordinate(
                        0,
                        0,
                        0
                    )
                );

            updateSystem =
                new FluidUpdateSystem(
                    storage,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            scheduler =
                new FluidScheduler(
                    updateSystem,
                    FluidSimulationSettings.Default
                );

            coordinator =
                new FluidSimulationCoordinator(
                    storage,
                    scheduler,
                    FluidSimulationCoordinatorSettings.Default
                );
        }

        [TearDown]
        public void TearDown()
        {
            coordinator?.Dispose();

            storage?.Dispose();

            fluidDatabase?.Dispose();

            if (waterDefinition != null)
            {
                Object.DestroyImmediate(
                    waterDefinition
                );
            }
        }

        [Test]
        public void StartsWithNoRunningSimulations()
        {
            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(0)
            );

            Assert.That(
                coordinator.HasRunningSimulations,
                Is.False
            );
        }

        [Test]
        public void CanScheduleChunk()
        {
            bool scheduled =
                coordinator.TrySchedule(
                    chunk.Coordinate,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                scheduled,
                Is.True
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(1)
            );

            Assert.That(
                coordinator.IsRunning(
                    chunk.Coordinate
                ),
                Is.True
            );
        }

        [Test]
        public void CannotScheduleSameChunkTwice()
        {
            bool first =
                coordinator.TrySchedule(
                    chunk.Coordinate,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            bool second =
                coordinator.TrySchedule(
                    chunk.Coordinate,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                first,
                Is.True
            );

            Assert.That(
                second,
                Is.False
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void CannotScheduleMissingChunk()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    99,
                    0,
                    99
                );

            bool scheduled =
                coordinator.TrySchedule(
                    coordinate,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                scheduled,
                Is.False
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void CompleteMarksSimulationFinished()
        {
            coordinator.TrySchedule(
                chunk.Coordinate,
                fluidDatabase,
                FluidSimulationSettings.Default
            );

            bool completed =
                coordinator.TryComplete(
                    chunk.Coordinate
                );

            Assert.That(
                completed,
                Is.True
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void CompleteAndEnqueueRemovesRunner()
        {
            coordinator.TrySchedule(
                chunk.Coordinate,
                fluidDatabase,
                FluidSimulationSettings.Default
            );

            int added =
                coordinator.CompleteAndEnqueue(
                    chunk.Coordinate
                );

            Assert.That(
                added,
                Is.GreaterThanOrEqualTo(0)
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(0)
            );

            Assert.That(
                coordinator.IsRunning(
                    chunk.Coordinate
                ),
                Is.False
            );
        }

        [Test]
        public void CompleteAllRemovesAllRunners()
        {
            Chunk secondChunk =
                storage.Create(
                    new ChunkCoordinate(
                        1,
                        0,
                        0
                    )
                );

            coordinator.TrySchedule(
                chunk.Coordinate,
                fluidDatabase,
                FluidSimulationSettings.Default
            );

            coordinator.TrySchedule(
                secondChunk.Coordinate,
                fluidDatabase,
                FluidSimulationSettings.Default
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(2)
            );

            coordinator.CompleteAll();

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void RemoveCancelsRunningSimulation()
        {
            coordinator.TrySchedule(
                chunk.Coordinate,
                fluidDatabase,
                FluidSimulationSettings.Default
            );

            bool removed =
                coordinator.Remove(
                    chunk.Coordinate
                );

            Assert.That(
                removed,
                Is.True
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(0)
            );

            Assert.That(
                coordinator.IsRunning(
                    chunk.Coordinate
                ),
                Is.False
            );
        }

        [Test]
        public void RemoveMissingSimulationReturnsFalse()
        {
            bool removed =
                coordinator.Remove(
                    chunk.Coordinate
                );

            Assert.That(
                removed,
                Is.False
            );
        }

        [Test]
        public void ClearRemovesAllRunningSimulations()
        {
            coordinator.TrySchedule(
                chunk.Coordinate,
                fluidDatabase,
                FluidSimulationSettings.Default
            );

            coordinator.Clear();

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void DisposeCanBeCalledTwice()
        {
            coordinator.Dispose();
            coordinator.Dispose();

            Assert.Pass();
        }

        private static FluidDefinition CreateFluidDefinition(
            FluidType type,
            string fluidName,
            ushort blockId,
            byte maxLevel,
            byte horizontalDecay,
            byte verticalDecay,
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
                maxLevel;

            serialized.FindProperty(
                "horizontalFlowDecay"
            ).intValue =
                horizontalDecay;

            serialized.FindProperty(
                "verticalFlowDecay"
            ).intValue =
                verticalDecay;

            serialized.FindProperty(
                "isLava"
            ).boolValue =
                isLava;

            serialized.ApplyModifiedPropertiesWithoutUndo();

            return definition;
        }

        [Test]
        public void MaxConcurrentSimulationsIsRespected()
        {
            FluidSimulationCoordinatorSettings settings =
                new FluidSimulationCoordinatorSettings
                {
                    MaxConcurrentSimulations = 2
                };

            FluidSimulationCoordinator limitedCoordinator =
                new FluidSimulationCoordinator(
                    storage,
                    scheduler,
                    settings
                );

            Chunk secondChunk =
                storage.Create(
                    new ChunkCoordinate(
                        1,
                        0,
                        0
                    )
                );

            Chunk thirdChunk =
                storage.Create(
                    new ChunkCoordinate(
                        2,
                        0,
                        0
                    )
                );

            Assert.That(
                limitedCoordinator.TrySchedule(
                    chunk.Coordinate,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                ),
                Is.True
            );

            Assert.That(
                limitedCoordinator.TrySchedule(
                    secondChunk.Coordinate,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                ),
                Is.True
            );

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(2)
            );

            Assert.That(
                limitedCoordinator.TrySchedule(
                    thirdChunk.Coordinate,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                ),
                Is.False
            );

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(2)
            );

            limitedCoordinator.Dispose();
        }

        [Test]
        public void CompletingSimulationAllowsAnotherSimulation()
        {
            FluidSimulationCoordinatorSettings settings =
                new FluidSimulationCoordinatorSettings
                {
                    MaxConcurrentSimulations = 1
                };

            FluidSimulationCoordinator limitedCoordinator =
                new FluidSimulationCoordinator(
                    storage,
                    scheduler,
                    settings
                );

            Chunk secondChunk =
                storage.Create(
                    new ChunkCoordinate(
                        1,
                        0,
                        0
                    )
                );

            Assert.That(
                limitedCoordinator.TrySchedule(
                    chunk.Coordinate,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                ),
                Is.True
            );

            Assert.That(
                limitedCoordinator.TrySchedule(
                    secondChunk.Coordinate,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                ),
                Is.False
            );

            int added =
                limitedCoordinator.CompleteAndEnqueue(
                    chunk.Coordinate
                );

            Assert.That(
                added,
                Is.GreaterThanOrEqualTo(0)
            );

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(0)
            );

            Assert.That(
                limitedCoordinator.TrySchedule(
                    secondChunk.Coordinate,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                ),
                Is.True
            );

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(1)
            );

            limitedCoordinator.Dispose();
        }

        [Test]
        public void SameChunkCannotConsumeMultipleSimulationSlots()
        {
            FluidSimulationCoordinatorSettings settings =
                new FluidSimulationCoordinatorSettings
                {
                    MaxConcurrentSimulations = 4
                };

            FluidSimulationCoordinator limitedCoordinator =
                new FluidSimulationCoordinator(
                    storage,
                    scheduler,
                    settings
                );

            Assert.That(
                limitedCoordinator.TrySchedule(
                    chunk.Coordinate,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                ),
                Is.True
            );

            Assert.That(
                limitedCoordinator.TrySchedule(
                    chunk.Coordinate,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                ),
                Is.False
            );

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(1)
            );

            Assert.That(
                limitedCoordinator.IsRunning(
                    chunk.Coordinate
                ),
                Is.True
            );

            limitedCoordinator.Dispose();
        }
    }
}
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
        public void StartsWithNoPendingSimulationRequests()
        {
            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );

            Assert.That(
                coordinator.HasPendingSimulations,
                Is.False
            );
        }

        [Test]
        public void CanRequestSimulation()
        {
            FluidSimulationRequest request =
                new FluidSimulationRequest(
                    chunk.Coordinate
                );

            bool requested =
                coordinator.RequestSimulation(
                    request
                );

            Assert.That(
                requested,
                Is.True
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );

            Assert.That(
                coordinator.HasPendingSimulations,
                Is.True
            );
        }

        [Test]
        public void CannotRequestSameSimulationTwice()
        {
            FluidSimulationRequest request =
                new FluidSimulationRequest(
                    chunk.Coordinate
                );

            bool first =
                coordinator.RequestSimulation(
                    request
                );

            bool second =
                coordinator.RequestSimulation(
                    request
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
                coordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void DifferentChunksCanBeRequested()
        {
            Chunk secondChunk =
                storage.Create(
                    new ChunkCoordinate(
                        1,
                        0,
                        0
                    )
                );

            FluidSimulationRequest firstRequest =
                new FluidSimulationRequest(
                    chunk.Coordinate
                );

            FluidSimulationRequest secondRequest =
                new FluidSimulationRequest(
                    secondChunk.Coordinate
                );

            Assert.That(
                coordinator.RequestSimulation(
                    firstRequest
                ),
                Is.True
            );

            Assert.That(
                coordinator.RequestSimulation(
                    secondRequest
                ),
                Is.True
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void PendingRequestDoesNotConsumeRunningSlot()
        {
            FluidSimulationRequest request =
                new FluidSimulationRequest(
                    chunk.Coordinate
                );

            coordinator.RequestSimulation(
                request
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(0)
            );

            Assert.That(
                coordinator.CanSchedule,
                Is.True
            );
        }

        [Test]
        public void ClearRemovesPendingSimulationRequests()
        {
            coordinator.RequestSimulation(
                new FluidSimulationRequest(
                    chunk.Coordinate
                )
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );

            coordinator.Clear();

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );

            Assert.That(
                coordinator.HasPendingSimulations,
                Is.False
            );
        }

        [Test]
        public void TryScheduleNextReturnsFalseWithoutRequests()
        {
            bool scheduled =
                coordinator.TryScheduleNext(
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

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void TryScheduleNextSchedulesRequestedChunk()
        {
            FluidSimulationRequest request =
                new FluidSimulationRequest(
                    chunk.Coordinate
                );

            coordinator.RequestSimulation(
                request
            );

            bool scheduled =
                coordinator.TryScheduleNext(
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
        public void TryScheduleNextConsumesRequest()
        {
            coordinator.RequestSimulation(
                new FluidSimulationRequest(
                    chunk.Coordinate
                )
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );

            bool scheduled =
                coordinator.TryScheduleNext(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                scheduled,
                Is.True
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );

            Assert.That(
                coordinator.HasPendingSimulations,
                Is.False
            );
        }

        [Test]
        public void TryScheduleNextRespectsConcurrencyLimit()
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

            limitedCoordinator.RequestSimulation(
                new FluidSimulationRequest(
                    chunk.Coordinate
                )
            );

            limitedCoordinator.RequestSimulation(
                new FluidSimulationRequest(
                    secondChunk.Coordinate
                )
            );

            Assert.That(
                limitedCoordinator.TryScheduleNext(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                ),
                Is.True
            );

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(1)
            );

            Assert.That(
                limitedCoordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );

            Assert.That(
                limitedCoordinator.TryScheduleNext(
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
                limitedCoordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );

            limitedCoordinator.Dispose();
        }

        [Test]
        public void TryScheduleNextDoesNotDuplicateRunningChunk()
        {
            coordinator.RequestSimulation(
                new FluidSimulationRequest(
                    chunk.Coordinate
                )
            );

            Assert.That(
                coordinator.TryScheduleNext(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                ),
                Is.True
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(1)
            );

            bool requestedAgain =
                coordinator.RequestSimulation(
                    new FluidSimulationRequest(
                        chunk.Coordinate
                    )
                );

            Assert.That(
                requestedAgain,
                Is.True
            );

            bool scheduledAgain =
                coordinator.TryScheduleNext(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                scheduledAgain,
                Is.False
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(1)
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void TryScheduleNextRemovesMissingChunkRequest()
        {
            ChunkCoordinate missingCoordinate =
                new ChunkCoordinate(
                    99,
                    0,
                    99
                );

            coordinator.RequestSimulation(
                new FluidSimulationRequest(
                    missingCoordinate
                )
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );

            bool scheduled =
                coordinator.TryScheduleNext(
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

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );

            Assert.That(
                coordinator.HasPendingSimulations,
                Is.False
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

        [Test]
        public void CompleteFinishedProcessesCompletedSimulation()
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

            bool completed =
                coordinator.TryComplete(
                    chunk.Coordinate
                );

            Assert.That(
                completed,
                Is.True
            );

            int added =
                coordinator.CompleteFinished();

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
        public void CompleteFinishedReturnsZeroWhenNothingIsRunning()
        {
            Debug.Log(
                "TEST NUEVO: CompleteFinishedReturnsZeroWhenNothingIsRunning"
            );
            int added =
                coordinator.CompleteFinished();

            Assert.That(
                added,
                Is.EqualTo(0)
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void SchedulePendingReturnsZeroWithoutRequests()
        {
            int scheduled =
                coordinator.SchedulePending(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                scheduled,
                Is.EqualTo(0)
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(0)
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void SchedulePendingSchedulesAllAvailableSlots()
        {
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

            Chunk fourthChunk =
                storage.Create(
                    new ChunkCoordinate(
                        3,
                        0,
                        0
                    )
                );

            Chunk fifthChunk =
                storage.Create(
                    new ChunkCoordinate(
                        4,
                        0,
                        0
                    )
                );

            coordinator.RequestSimulation(
                new FluidSimulationRequest(
                    chunk.Coordinate
                )
            );

            coordinator.RequestSimulation(
                new FluidSimulationRequest(
                    secondChunk.Coordinate
                )
            );

            coordinator.RequestSimulation(
                new FluidSimulationRequest(
                    thirdChunk.Coordinate
                )
            );

            coordinator.RequestSimulation(
                new FluidSimulationRequest(
                    fourthChunk.Coordinate
                )
            );

            coordinator.RequestSimulation(
                new FluidSimulationRequest(
                    fifthChunk.Coordinate
                )
            );

            int scheduled =
                coordinator.SchedulePending(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                scheduled,
                Is.EqualTo(4)
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(4)
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void SchedulePendingDoesNotExceedConcurrencyLimit()
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

            limitedCoordinator.RequestSimulation(
                new FluidSimulationRequest(
                    chunk.Coordinate
                )
            );

            limitedCoordinator.RequestSimulation(
                new FluidSimulationRequest(
                    secondChunk.Coordinate
                )
            );

            limitedCoordinator.RequestSimulation(
                new FluidSimulationRequest(
                    thirdChunk.Coordinate
                )
            );

            int scheduled =
                limitedCoordinator.SchedulePending(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                scheduled,
                Is.EqualTo(2)
            );

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(2)
            );

            Assert.That(
                limitedCoordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );

            limitedCoordinator.Dispose();
        }

        [Test]
        public void SchedulePendingKeepsRemainingRequestsQueued()
        {
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

            coordinator.RequestSimulation(
                new FluidSimulationRequest(
                    chunk.Coordinate
                )
            );

            coordinator.RequestSimulation(
                new FluidSimulationRequest(
                    secondChunk.Coordinate
                )
            );

            coordinator.RequestSimulation(
                new FluidSimulationRequest(
                    thirdChunk.Coordinate
                )
            );

            int scheduled =
                coordinator.SchedulePending(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                scheduled,
                Is.EqualTo(3)
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(3)
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void SchedulePendingSkipsMissingChunkAndSchedulesNextValidRequest()
        {
            ChunkCoordinate missingCoordinate =
                new ChunkCoordinate(
                    99,
                    0,
                    99
                );

            coordinator.RequestSimulation(
                new FluidSimulationRequest(
                    missingCoordinate
                )
            );

            coordinator.RequestSimulation(
                new FluidSimulationRequest(
                    chunk.Coordinate
                )
            );

            int scheduled =
                coordinator.SchedulePending(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                scheduled,
                Is.EqualTo(1)
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(1)
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );

            Assert.That(
                coordinator.IsRunning(
                    chunk.Coordinate
                ),
                Is.True
            );
        }

        [Test]
        public void SchedulePendingCanFillFreedSlots()
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

            limitedCoordinator.RequestSimulation(
                new FluidSimulationRequest(
                    chunk.Coordinate
                )
            );

            limitedCoordinator.RequestSimulation(
                new FluidSimulationRequest(
                    secondChunk.Coordinate
                )
            );

            limitedCoordinator.RequestSimulation(
                new FluidSimulationRequest(
                    thirdChunk.Coordinate
                )
            );

            int firstScheduled =
                limitedCoordinator.SchedulePending(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                firstScheduled,
                Is.EqualTo(2)
            );

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(2)
            );

            Assert.That(
                limitedCoordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );

            limitedCoordinator.CompleteAll();

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(0)
            );

            int secondScheduled =
                limitedCoordinator.SchedulePending(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                secondScheduled,
                Is.EqualTo(1)
            );

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(1)
            );

            Assert.That(
                limitedCoordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );

            limitedCoordinator.Dispose();
        }

        [Test]
        public void CompleteFinishedAndSchedulePendingReturnsZeroWhenIdle()
        {
            int processed =
                coordinator.CompleteFinishedAndSchedulePending(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                processed,
                Is.EqualTo(0)
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(0)
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void CompleteFinishedAndSchedulePendingFillsFreedSlots()
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

            limitedCoordinator.RequestSimulation(
                new FluidSimulationRequest(
                    chunk.Coordinate
                )
            );

            limitedCoordinator.RequestSimulation(
                new FluidSimulationRequest(
                    secondChunk.Coordinate
                )
            );

            limitedCoordinator.RequestSimulation(
                new FluidSimulationRequest(
                    thirdChunk.Coordinate
                )
            );

            int firstScheduled =
                limitedCoordinator.SchedulePending(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                firstScheduled,
                Is.EqualTo(2)
            );

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(2)
            );

            Assert.That(
                limitedCoordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );

            /*
            * Esperamos a que los dos Jobs terminen.
            */
            limitedCoordinator.CompleteAll();

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(0)
            );

            Assert.That(
                limitedCoordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );

            /*
            * Como ahora hay dos slots libres,
            * debe ejecutarse la request restante.
            */
            int processed =
                limitedCoordinator.CompleteFinishedAndSchedulePending(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                processed,
                Is.EqualTo(1)
            );

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(1)
            );

            Assert.That(
                limitedCoordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );

            limitedCoordinator.Dispose();
        }

        [Test]
        public void CompleteFinishedAndSchedulePendingCanRecycleSimulationSlots()
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

            Chunk fourthChunk =
                storage.Create(
                    new ChunkCoordinate(
                        3,
                        0,
                        0
                    )
                );

            limitedCoordinator.RequestSimulation(
                new FluidSimulationRequest(
                    chunk.Coordinate
                )
            );

            limitedCoordinator.RequestSimulation(
                new FluidSimulationRequest(
                    secondChunk.Coordinate
                )
            );

            limitedCoordinator.RequestSimulation(
                new FluidSimulationRequest(
                    thirdChunk.Coordinate
                )
            );

            limitedCoordinator.RequestSimulation(
                new FluidSimulationRequest(
                    fourthChunk.Coordinate
                )
            );

            int firstBatch =
                limitedCoordinator.SchedulePending(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                firstBatch,
                Is.EqualTo(2)
            );

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(2)
            );

            Assert.That(
                limitedCoordinator.PendingSimulationCount,
                Is.EqualTo(2)
            );

            /*
            * Terminamos el primer lote.
            */
            limitedCoordinator.CompleteAll();

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(0)
            );

            /*
            * Volvemos a llenar los dos slots.
            */
            int secondBatch =
                limitedCoordinator.SchedulePending(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.That(
                secondBatch,
                Is.EqualTo(2)
            );

            Assert.That(
                limitedCoordinator.RunningCount,
                Is.EqualTo(2)
            );

            Assert.That(
                limitedCoordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );

            limitedCoordinator.Dispose();
        }

        [Test]
        public void CompleteAndEnqueueAddsSimulationResultsToScheduler()
        {
            ChunkDataAccess.SetVoxel(
                chunk.Data,
                8,
                8,
                8,
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                )
            );

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

            int added =
                coordinator.CompleteAndEnqueue(
                    chunk.Coordinate
                );

            Assert.That(
                added,
                Is.GreaterThan(0)
            );

            Assert.That(
                scheduler.PendingCount,
                Is.GreaterThan(0)
            );

            Assert.That(
                coordinator.RunningCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void SchedulerAppliesSimulationResultToChunk()
        {
            ChunkDataAccess.SetVoxel(
                chunk.Data,
                8,
                8,
                8,
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                )
            );

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

            int added =
                coordinator.CompleteAndEnqueue(
                    chunk.Coordinate
                );

            Assert.That(
                added,
                Is.GreaterThan(0)
            );

            int processed =
                scheduler.ProcessTick();

            Assert.That(
                processed,
                Is.GreaterThan(0)
            );

            Voxel below =
                ChunkDataAccess.GetVoxel(
                    chunk.Data,
                    8,
                    7,
                    8
                );

            Assert.That(
                below.BlockId,
                Is.EqualTo(WaterBlockId)
            );

            Assert.That(
                below.State,
                Is.GreaterThan(0)
            );
        }

        [Test]
        public void ApplyingSimulationResultMarksChunkDirty()
        {
            ChunkDataAccess.SetVoxel(
                chunk.Data,
                8,
                8,
                8,
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                )
            );

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

            coordinator.CompleteAndEnqueue(
                chunk.Coordinate
            );

            uint revisionBefore =
                chunk.DataRevision;

            int processed =
                scheduler.ProcessTick();

            Assert.That(
                processed,
                Is.GreaterThan(0)
            );

            Assert.That(
                chunk.IsDirty,
                Is.True
            );

            Assert.That(
                chunk.NeedsMesh,
                Is.True
            );

            Assert.That(
                chunk.NeedsSave,
                Is.True
            );

            Assert.That(
                chunk.DataRevision,
                Is.GreaterThan(
                    revisionBefore
                )
            );
        }

        [Test]
        public void TryScheduleNextKeepsRequestWhenConcurrencyLimitIsReached()
        {
            FluidSimulationCoordinatorSettings settings =
                FluidSimulationCoordinatorSettings.Default;

            settings.MaxConcurrentSimulations = 1;

            FluidSimulationCoordinator limitedCoordinator =
                new FluidSimulationCoordinator(
                    storage,
                    scheduler,
                    settings
                );

            try
            {
                ChunkCoordinate first =
                    new ChunkCoordinate(
                        0,
                        0,
                        0
                    );

                ChunkCoordinate second =
                    new ChunkCoordinate(
                        1,
                        0,
                        0
                    );

                storage.Create(second);

                Assert.IsTrue(
                    limitedCoordinator.TrySchedule(
                        first,
                        fluidDatabase,
                        FluidSimulationSettings.Default
                    )
                );

                Assert.IsTrue(
                    limitedCoordinator.RequestSimulation(
                        new FluidSimulationRequest(second)
                    )
                );

                Assert.IsFalse(
                    limitedCoordinator.TryScheduleNext(
                        fluidDatabase,
                        FluidSimulationSettings.Default
                    )
                );

                Assert.AreEqual(
                    1,
                    limitedCoordinator.PendingSimulationCount
                );
            }
            finally
            {
                limitedCoordinator.Dispose();
            }
        }

        [Test]
        public void TryScheduleNextRemovesRequestForAlreadyRunningChunk()
        {
            Assert.IsTrue(
                coordinator.TrySchedule(
                    chunk.Coordinate,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                )
            );

            Assert.IsTrue(
                coordinator.RequestSimulation(
                    new FluidSimulationRequest(
                        chunk.Coordinate
                    )
                )
            );

            Assert.IsFalse(
                coordinator.TryScheduleNext(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                )
            );

            Assert.AreEqual(
                0,
                coordinator.PendingSimulationCount
            );

            Assert.IsTrue(
                coordinator.IsRunning(
                    chunk.Coordinate
                )
            );
        }

        [Test]
        public void TryScheduleNextConsumesValidRequest()
        {
            Assert.IsTrue(
                coordinator.RequestSimulation(
                    new FluidSimulationRequest(
                        chunk.Coordinate
                    )
                )
            );

            Assert.AreEqual(
                1,
                coordinator.PendingSimulationCount
            );

            bool scheduled =
                coordinator.TryScheduleNext(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );

            Assert.IsTrue(
                scheduled
            );

            Assert.AreEqual(
                0,
                coordinator.PendingSimulationCount
            );

            Assert.IsTrue(
                coordinator.IsRunning(
                    chunk.Coordinate
                )
            );
        }

        [Test]
        public void TryScheduleNextProcessesRequestsInQueueOrder()
        {
            ChunkCoordinate secondCoordinate =
                new ChunkCoordinate(
                    1,
                    0,
                    0
                );

            storage.Create(
                secondCoordinate
            );

            Assert.IsTrue(
                coordinator.RequestSimulation(
                    new FluidSimulationRequest(
                        chunk.Coordinate
                    )
                )
            );

            Assert.IsTrue(
                coordinator.RequestSimulation(
                    new FluidSimulationRequest(
                        secondCoordinate
                    )
                )
            );

            Assert.IsTrue(
                coordinator.TryScheduleNext(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                )
            );

            Assert.IsTrue(
                coordinator.IsRunning(
                    chunk.Coordinate
                )
            );

            Assert.AreEqual(
                1,
                coordinator.PendingSimulationCount
            );

            Assert.IsTrue(
                coordinator.TryScheduleNext(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                )
            );

            Assert.IsTrue(
                coordinator.IsRunning(
                    secondCoordinate
                )
            );

            Assert.AreEqual(
                0,
                coordinator.PendingSimulationCount
            );
        }

        [Test]
        public void TryScheduleNextDoesNotConsumeRequestWhenSchedulingFails()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    10,
                    0,
                    10
                );

            Assert.IsTrue(
                coordinator.RequestSimulation(
                    new FluidSimulationRequest(
                        coordinate
                    )
                )
            );

            Assert.IsFalse(
                coordinator.TryScheduleNext(
                    fluidDatabase,
                    FluidSimulationSettings.Default
                )
            );

            Assert.AreEqual(
                0,
                coordinator.RunningCount
            );

            Assert.AreEqual(
                0,
                coordinator.PendingSimulationCount
            );
        }

        [Test]
        public void RequestChunkSimulationReturnsFalseForMissingChunk()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(50, 0, 50);

            Assert.That(
                coordinator.RequestChunkSimulation(
                    coordinate,
                    fluidDatabase),
                Is.False
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void RequestChunkSimulationReturnsFalseForChunkWithoutFluids()
        {
            chunk.SetState(ChunkState.Generated);

            Assert.That(
                coordinator.RequestChunkSimulation(
                    chunk.Coordinate,
                    fluidDatabase
                ),
                Is.False
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void RequestChunkSimulationRequestsGeneratedChunkWithFluids()
        {
            chunk.SetState(ChunkState.Generated);

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                0,
                0,
                0,
                new Voxel(
                    WaterBlockId,
                    0,
                    15
                )
            );

            Assert.That(
                coordinator.RequestChunkSimulation(
                    chunk.Coordinate,
                    fluidDatabase
                ),
                Is.True
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void RequestChunkSimulationRequestsReadyChunkWithFluids()
        {
            chunk.SetState(ChunkState.Ready);

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                0,
                0,
                0,
                new Voxel(
                    WaterBlockId,
                    0,
                    15
                )
            );

            Assert.That(
                coordinator.RequestChunkSimulation(
                    chunk.Coordinate,
                    fluidDatabase
                ),
                Is.True
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void RequestChunkSimulationReturnsFalseForGeneratingChunk()
        {
            chunk.SetState(ChunkState.Generating);

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                0,
                0,
                0,
                new Voxel(
                    WaterBlockId,
                    0,
                    15
                )
            );

            Assert.That(
                coordinator.RequestChunkSimulation(
                    chunk.Coordinate,
                    fluidDatabase
                ),
                Is.False
            );
        }

        [Test]
        public void RequestChunkSimulationDoesNotDuplicateRequest()
        {
            chunk.SetState(ChunkState.Generated);

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                0,
                0,
                0,
                new Voxel(
                    WaterBlockId,
                    0,
                    15
                )
            );

            Assert.That(
                coordinator.RequestChunkSimulation(
                    chunk.Coordinate,
                    fluidDatabase
                ),
                Is.True
            );

            Assert.That(
                coordinator.RequestChunkSimulation(
                    chunk.Coordinate,
                    fluidDatabase
                ),
                Is.False
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );
        }
    } 
}
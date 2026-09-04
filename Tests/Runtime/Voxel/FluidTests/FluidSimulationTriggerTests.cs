using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace WildEarth.Voxel.Tests
{
    public sealed class FluidSimulationTriggerTests
    {
        private FluidDefinition waterDefinition;

        private FluidRuntimeDatabase fluidDatabase;

        private ChunkDataPool dataPool;
        private ChunkBiomeDataPool biomeDataPool;

        private ChunkStorage storage;

        private FluidUpdateSystem updateSystem;
        private FluidScheduler scheduler;

        private FluidSimulationCoordinator coordinator;
        private FluidSimulationTrigger trigger;

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

            trigger =
                new FluidSimulationTrigger(
                    coordinator,
                    fluidDatabase
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
        public void TriggerReturnsFalseForMissingChunk()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(
                    50,
                    0,
                    50
                );

            bool triggered =
                trigger.Trigger(
                    coordinate
                );

            Assert.That(
                triggered,
                Is.False
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void TriggerReturnsFalseForChunkWithoutFluids()
        {
            chunk.SetState(
                ChunkState.Generated
            );

            bool triggered =
                trigger.Trigger(
                    chunk.Coordinate
                );

            Assert.That(
                triggered,
                Is.False
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void TriggerRequestsGeneratedChunkWithFluids()
        {
            chunk.SetState(
                ChunkState.Generated
            );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                0,
                0,
                0,
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                )
            );

            bool triggered =
                trigger.Trigger(
                    chunk.Coordinate
                );

            Assert.That(
                triggered,
                Is.True
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void TriggerRequestsReadyChunkWithFluids()
        {
            chunk.SetState(
                ChunkState.Ready
            );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                0,
                0,
                0,
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                )
            );

            bool triggered =
                trigger.Trigger(
                    chunk.Coordinate
                );

            Assert.That(
                triggered,
                Is.True
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void TriggerReturnsFalseForChunkThatIsStillGenerating()
        {
            chunk.SetState(
                ChunkState.Generating
            );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                0,
                0,
                0,
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                )
            );

            bool triggered =
                trigger.Trigger(
                    chunk.Coordinate
                );

            Assert.That(
                triggered,
                Is.False
            );

            Assert.That(
                coordinator.PendingSimulationCount,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void TriggerDoesNotDuplicateSimulationRequest()
        {
            chunk.SetState(
                ChunkState.Generated
            );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                0,
                0,
                0,
                new Voxel(
                    WaterBlockId,
                    0,
                    FluidState.MaxLevel
                )
            );

            bool first =
                trigger.Trigger(
                    chunk.Coordinate
                );

            bool second =
                trigger.Trigger(
                    chunk.Coordinate
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
    }
}
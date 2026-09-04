using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace WildEarth.Voxel.Tests
{
    public sealed class FluidChunkUtilityTests
    {
        private const ushort WaterBlockId = 100;

        private FluidDefinition waterDefinition;
        private FluidRuntimeDatabase fluidDatabase;
        private ChunkData chunkData;

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
                    new[] { waterDefinition }
                );

            fluidDatabase =
                new FluidRuntimeDatabase(
                    registry,
                    Allocator.Persistent
                );

            chunkData =
                new ChunkData(
                    Allocator.Persistent
                );
        }

        [TearDown]
        public void TearDown()
        {
            if (chunkData != null)
                chunkData.Dispose();

            if (fluidDatabase != null)
                fluidDatabase.Dispose();

            if (waterDefinition != null)
                Object.DestroyImmediate(
                    waterDefinition
                );
        }

        [Test]
        public void EmptyChunkDoesNotContainFluids()
        {
            Assert.That(
                FluidChunkUtility.ContainsFluids(
                    chunkData,
                    fluidDatabase
                ),
                Is.False
            );
        }

        [Test]
        public void ChunkWithWaterContainsFluids()
        {
            ChunkDataAccess.SetVoxel(
                chunkData,
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
                FluidChunkUtility.ContainsFluids(
                    chunkData,
                    fluidDatabase
                ),
                Is.True
            );
        }

        [Test]
        public void ChunkWithEmptyFluidStateDoesNotContainFluids()
        {
            ChunkDataAccess.SetVoxel(
                chunkData,
                0,
                0,
                0,
                new Voxel(
                    WaterBlockId,
                    0,
                    0
                )
            );

            Assert.That(
                FluidChunkUtility.ContainsFluids(
                    chunkData,
                    fluidDatabase
                ),
                Is.False
            );
        }

        [Test]
        public void SolidBlockDoesNotCountAsFluid()
        {
            const ushort StoneBlockId = 200;

            ChunkDataAccess.SetVoxel(
                chunkData,
                0,
                0,
                0,
                new Voxel(
                    StoneBlockId,
                    0,
                    15
                )
            );

            Assert.That(
                FluidChunkUtility.ContainsFluids(
                    chunkData,
                    fluidDatabase
                ),
                Is.False
            );
        }

        [Test]
        public void FluidAtAnyPositionIsDetected()
        {
            ChunkDataAccess.SetVoxel(
                chunkData,
                7,
                12,
                15,
                new Voxel(
                    WaterBlockId,
                    0,
                    5
                )
            );

            Assert.That(
                FluidChunkUtility.ContainsFluids(
                    chunkData,
                    fluidDatabase
                ),
                Is.True
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
                ScriptableObject.CreateInstance<FluidDefinition>();

            SerializedObject serializedObject =
                new SerializedObject(definition);

            serializedObject.FindProperty("type").enumValueIndex =
                (int)type;

            serializedObject.FindProperty("fluidName").stringValue =
                fluidName;

            serializedObject.FindProperty("blockId").intValue =
                blockId;

            serializedObject.FindProperty("maxLevel").intValue =
                maxLevel;

            serializedObject.FindProperty("horizontalFlowDecay").intValue =
                horizontalDecay;

            serializedObject.FindProperty("verticalFlowDecay").intValue =
                verticalDecay;

            serializedObject.FindProperty("isLava").boolValue =
                isLava;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return definition;
        }
    }
}
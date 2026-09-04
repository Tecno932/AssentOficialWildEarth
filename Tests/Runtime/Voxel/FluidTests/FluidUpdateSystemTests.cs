using System;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace WildEarth.Voxel.Tests
{
    public sealed class FluidUpdateSystemTests
    {
        private ChunkDataPool dataPool;
        private ChunkBiomeDataPool biomeDataPool;
        private ChunkStorage chunkStorage;

        private FluidDefinition waterDefinition;
        private FluidDefinition lavaDefinition;

        private FluidRuntimeDatabase fluidDatabase;

        private FluidUpdateSystem system;

        [SetUp]
        public void SetUp()
        {
            dataPool =
                new ChunkDataPool(
                    Allocator.Persistent,
                    2,
                    8
                );

            biomeDataPool =
                new ChunkBiomeDataPool(
                    Allocator.Persistent,
                    2,
                    8
                );

            chunkStorage =
                new ChunkStorage(
                    dataPool,
                    biomeDataPool
                );

            waterDefinition =
                CreateFluidDefinition(
                    FluidType.Water,
                    "Water",
                    100,
                    false
                );

            lavaDefinition =
                CreateFluidDefinition(
                    FluidType.Lava,
                    "Lava",
                    101,
                    true
                );

            FluidRegistry registry =
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

            system =
                new FluidUpdateSystem(
                    chunkStorage,
                    fluidDatabase,
                    FluidSimulationSettings.Default
                );
        }

        [TearDown]
        public void TearDown()
        {
            if (fluidDatabase != null)
            {
                fluidDatabase.Dispose();
                fluidDatabase = null;
            }

            if (chunkStorage != null)
            {
                chunkStorage.Dispose();
                chunkStorage = null;
            }

            if (waterDefinition != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    waterDefinition
                );

                waterDefinition = null;
            }

            if (lavaDefinition != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    lavaDefinition
                );

                lavaDefinition = null;
            }

            system = null;
        }

        [Test]
        public void AppliesFluidToAir()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            Chunk chunk =
                chunkStorage.Create(
                    coordinate
                );

            FluidChange change =
                CreateWaterChange(
                    coordinate,
                    1,
                    2,
                    3,
                    10
                );

            bool applied =
                system.TryApply(
                    change,
                    out FluidChangeResult result
                );

            Assert.That(
                applied,
                Is.True
            );

            Assert.That(
                result.Applied,
                Is.True
            );

            Voxel voxel =
                ChunkDataAccess.GetVoxel(
                    chunk.Data,
                    1,
                    2,
                    3
                );

            Assert.That(
                voxel.BlockId,
                Is.EqualTo(100)
            );

            Assert.That(
                voxel.State,
                Is.EqualTo(10)
            );
        }

        [Test]
        public void RejectsWhenTargetChunkIsNotLoaded()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            FluidChange change =
                CreateWaterChange(
                    coordinate,
                    1,
                    2,
                    3,
                    10
                );

            bool applied =
                system.TryApply(
                    change,
                    out FluidChangeResult result
                );

            Assert.That(
                applied,
                Is.False
            );

            Assert.That(
                result.Applied,
                Is.False
            );

            Assert.That(
                result.TargetChunkLoaded,
                Is.False
            );
        }

        [Test]
        public void RejectsInvalidLocalCoordinate()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            chunkStorage.Create(
                coordinate
            );

            FluidChange change =
                CreateWaterChange(
                    coordinate,
                    -1,
                    2,
                    3,
                    10
                );

            bool applied =
                system.TryApply(
                    change,
                    out FluidChangeResult result
                );

            Assert.That(
                applied,
                Is.False
            );

            Assert.That(
                result.Applied,
                Is.False
            );

            Assert.That(
                result.TargetChunkLoaded,
                Is.True
            );
        }

        [Test]
        public void RejectsSolidTarget()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            Chunk chunk =
                chunkStorage.Create(
                    coordinate
                );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                1,
                2,
                3,
                new Voxel(200)
            );

            FluidChange change =
                CreateWaterChange(
                    coordinate,
                    1,
                    2,
                    3,
                    10
                );

            bool applied =
                system.TryApply(
                    change,
                    out FluidChangeResult result
                );

            Assert.That(
                applied,
                Is.False
            );

            Assert.That(
                result.Applied,
                Is.False
            );

            Voxel voxel =
                ChunkDataAccess.GetVoxel(
                    chunk.Data,
                    1,
                    2,
                    3
                );

            Assert.That(
                voxel.BlockId,
                Is.EqualTo(200)
            );
        }

        [Test]
        public void ReplacesWeakerSameFluid()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            Chunk chunk =
                chunkStorage.Create(
                    coordinate
                );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                1,
                2,
                3,
                new Voxel(
                    100,
                    0,
                    5
                )
            );

            FluidChange change =
                CreateWaterChange(
                    coordinate,
                    1,
                    2,
                    3,
                    10
                );

            bool applied =
                system.TryApply(
                    change,
                    out FluidChangeResult result
                );

            Assert.That(
                applied,
                Is.True
            );

            Assert.That(
                result.ReplacedWeakerFluid,
                Is.True
            );

            Voxel voxel =
                ChunkDataAccess.GetVoxel(
                    chunk.Data,
                    1,
                    2,
                    3
                );

            Assert.That(
                voxel.State,
                Is.EqualTo(10)
            );
        }

        [Test]
        public void RejectsEqualFluidLevel()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            Chunk chunk =
                chunkStorage.Create(
                    coordinate
                );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                1,
                2,
                3,
                new Voxel(
                    100,
                    0,
                    10
                )
            );

            FluidChange change =
                CreateWaterChange(
                    coordinate,
                    1,
                    2,
                    3,
                    10
                );

            bool applied =
                system.TryApply(
                    change,
                    out FluidChangeResult result
                );

            Assert.That(
                applied,
                Is.False
            );

            Assert.That(
                result.Applied,
                Is.False
            );
        }

        [Test]
        public void RejectsWeakerFluidLevel()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            Chunk chunk =
                chunkStorage.Create(
                    coordinate
                );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                1,
                2,
                3,
                new Voxel(
                    100,
                    0,
                    10
                )
            );

            FluidChange change =
                CreateWaterChange(
                    coordinate,
                    1,
                    2,
                    3,
                    5
                );

            bool applied =
                system.TryApply(
                    change,
                    out FluidChangeResult result
                );

            Assert.That(
                applied,
                Is.False
            );

            Assert.That(
                result.Applied,
                Is.False
            );
        }

        [Test]
        public void RejectsDifferentFluid()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            Chunk chunk =
                chunkStorage.Create(
                    coordinate
                );

            ChunkDataAccess.SetVoxel(
                chunk.Data,
                1,
                2,
                3,
                new Voxel(
                    100,
                    0,
                    10
                )
            );

            FluidChange change =
                new FluidChange(
                    coordinate,
                    1,
                    2,
                    3,
                    new FluidState(
                        FluidType.Lava,
                        15
                    )
                );

            bool applied =
                system.TryApply(
                    change,
                    out FluidChangeResult result
                );

            Assert.That(
                applied,
                Is.False
            );

            Assert.That(
                result.Applied,
                Is.False
            );

            Voxel voxel =
                ChunkDataAccess.GetVoxel(
                    chunk.Data,
                    1,
                    2,
                    3
                );

            Assert.That(
                voxel.BlockId,
                Is.EqualTo(100)
            );

            Assert.That(
                voxel.State,
                Is.EqualTo(10)
            );
        }

        [Test]
        public void WritesCorrectBlockId()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            Chunk chunk =
                chunkStorage.Create(
                    coordinate
                );

            FluidChange change =
                CreateLavaChange(
                    coordinate,
                    4,
                    5,
                    6,
                    15
                );

            system.TryApply(
                change,
                out _
            );

            Voxel voxel =
                ChunkDataAccess.GetVoxel(
                    chunk.Data,
                    4,
                    5,
                    6
                );

            Assert.That(
                voxel.BlockId,
                Is.EqualTo(101)
            );
        }

        [Test]
        public void WritesCorrectFluidStateLevel()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            Chunk chunk =
                chunkStorage.Create(
                    coordinate
                );

            FluidChange change =
                CreateWaterChange(
                    coordinate,
                    4,
                    5,
                    6,
                    7
                );

            system.TryApply(
                change,
                out _
            );

            Voxel voxel =
                ChunkDataAccess.GetVoxel(
                    chunk.Data,
                    4,
                    5,
                    6
                );

            Assert.That(
                voxel.State,
                Is.EqualTo(7)
            );
        }

        [Test]
        public void MarksChunkDirty()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            Chunk chunk =
                chunkStorage.Create(
                    coordinate
                );

            FluidChange change =
                CreateWaterChange(
                    coordinate,
                    1,
                    2,
                    3,
                    10
                );

            system.TryApply(
                change,
                out _
            );

            Assert.That(
                chunk.IsDirty,
                Is.True
            );
        }

        [Test]
        public void MarksChunkNeedsMesh()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            Chunk chunk =
                chunkStorage.Create(
                    coordinate
                );

            FluidChange change =
                CreateWaterChange(
                    coordinate,
                    1,
                    2,
                    3,
                    10
                );

            system.TryApply(
                change,
                out _
            );

            Assert.That(
                chunk.NeedsMesh,
                Is.True
            );
        }

        [Test]
        public void MarksChunkNeedsSave()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            Chunk chunk =
                chunkStorage.Create(
                    coordinate
                );

            FluidChange change =
                CreateWaterChange(
                    coordinate,
                    1,
                    2,
                    3,
                    10
                );

            system.TryApply(
                change,
                out _
            );

            Assert.That(
                chunk.NeedsSave,
                Is.True
            );
        }

        [Test]
        public void IncrementsDataRevision()
        {
            ChunkCoordinate coordinate =
                new ChunkCoordinate(0, 0, 0);

            Chunk chunk =
                chunkStorage.Create(
                    coordinate
                );

            uint initialRevision =
                chunk.DataRevision;

            FluidChange change =
                CreateWaterChange(
                    coordinate,
                    1,
                    2,
                    3,
                    10
                );

            system.TryApply(
                change,
                out _
            );

            Assert.That(
                chunk.DataRevision,
                Is.EqualTo(
                    initialRevision + 1
                )
            );
        }

        [Test]
        public void AppliesFluidToNeighborChunk()
        {
            ChunkCoordinate sourceCoordinate =
                new ChunkCoordinate(0, 0, 0);

            ChunkCoordinate targetCoordinate =
                new ChunkCoordinate(1, 0, 0);

            chunkStorage.Create(
                sourceCoordinate
            );

            Chunk targetChunk =
                chunkStorage.Create(
                    targetCoordinate
                );

            FluidChange change =
                CreateWaterChange(
                    targetCoordinate,
                    0,
                    5,
                    7,
                    8
                );

            bool applied =
                system.TryApply(
                    change,
                    out FluidChangeResult result
                );

            Assert.That(
                applied,
                Is.True
            );

            Assert.That(
                result.TargetChunkLoaded,
                Is.True
            );

            Voxel voxel =
                ChunkDataAccess.GetVoxel(
                    targetChunk.Data,
                    0,
                    5,
                    7
                );

            Assert.That(
                voxel.BlockId,
                Is.EqualTo(100)
            );

            Assert.That(
                voxel.State,
                Is.EqualTo(8)
            );
        }

        private static FluidChange CreateWaterChange(
            ChunkCoordinate coordinate,
            int x,
            int y,
            int z,
            byte level)
        {
            return new FluidChange(
                coordinate,
                x,
                y,
                z,
                new FluidState(
                    FluidType.Water,
                    level
                )
            );
        }

        private static FluidChange CreateLavaChange(
            ChunkCoordinate coordinate,
            int x,
            int y,
            int z,
            byte level)
        {
            return new FluidChange(
                coordinate,
                x,
                y,
                z,
                new FluidState(
                    FluidType.Lava,
                    level
                )
            );
        }

        private static FluidDefinition CreateFluidDefinition(
            FluidType type,
            string fluidName,
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
                fluidName
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
            FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic
                );

            if (field == null)
            {
                throw new InvalidOperationException(
                    $"No se encontró el campo privado " +
                    $"'{fieldName}' en " +
                    $"{target.GetType().Name}."
                );
            }

            field.SetValue(
                target,
                value
            );
        }
    }
}
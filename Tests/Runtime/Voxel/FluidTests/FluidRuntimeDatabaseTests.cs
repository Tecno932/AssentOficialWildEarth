using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace WildEarth.Voxel.Tests
{
    public sealed class FluidRuntimeDatabaseTests
    {
        private const ushort WaterBlockId = 10;
        private const ushort LavaBlockId = 11;
        private const ushort UnknownBlockId = 50;

        private FluidDefinition waterDefinition;
        private FluidDefinition lavaDefinition;

        private FluidRegistry registry;
        private FluidRuntimeDatabase database;

        [SetUp]
        public void SetUp()
        {
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

            database =
                new FluidRuntimeDatabase(
                    registry,
                    Allocator.Persistent
                );
        }

        [TearDown]
        public void TearDown()
        {
            if (database != null)
                database.Dispose();

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
        public void DatabaseIsCreated()
        {
            Assert.IsTrue(
                database.IsCreated
            );
        }

        [Test]
        public void DatabaseContainsAllFluids()
        {
            Assert.AreEqual(
                2,
                database.Length
            );
        }

        [Test]
        public void BlockLookupContainsWater()
        {
            bool found =
                database.TryGetByBlockId(
                    WaterBlockId,
                    out FluidRuntimeData fluid
                );

            Assert.IsTrue(found);

            Assert.AreEqual(
                FluidType.Water,
                fluid.Type
            );

            Assert.AreEqual(
                WaterBlockId,
                fluid.BlockId
            );
        }

        [Test]
        public void BlockLookupContainsLava()
        {
            bool found =
                database.TryGetByBlockId(
                    LavaBlockId,
                    out FluidRuntimeData fluid
                );

            Assert.IsTrue(found);

            Assert.AreEqual(
                FluidType.Lava,
                fluid.Type
            );

            Assert.AreEqual(
                LavaBlockId,
                fluid.BlockId
            );
        }

        [Test]
        public void BlockLookupRejectsUnknownBlock()
        {
            bool found =
                database.TryGetByBlockId(
                    UnknownBlockId,
                    out FluidRuntimeData fluid
                );

            Assert.IsFalse(found);
            Assert.IsFalse(fluid.IsValid);
        }

        [Test]
        public void BlockLookupRejectsAir()
        {
            bool found =
                database.TryGetByBlockId(
                    BlockIds.Air,
                    out FluidRuntimeData fluid
                );

            Assert.IsFalse(found);
            Assert.IsFalse(fluid.IsValid);
        }

        [Test]
        public void GetByBlockIdReturnsWater()
        {
            FluidRuntimeData fluid =
                database.GetByBlockId(
                    WaterBlockId
                );

            Assert.AreEqual(
                FluidType.Water,
                fluid.Type
            );

            Assert.AreEqual(
                WaterBlockId,
                fluid.BlockId
            );
        }

        [Test]
        public void GetByBlockIdReturnsLava()
        {
            FluidRuntimeData fluid =
                database.GetByBlockId(
                    LavaBlockId
                );

            Assert.AreEqual(
                FluidType.Lava,
                fluid.Type
            );

            Assert.AreEqual(
                LavaBlockId,
                fluid.BlockId
            );
        }

        [Test]
        public void GetByBlockIdReturnsDefaultForUnknownBlock()
        {
            FluidRuntimeData fluid =
                database.GetByBlockId(
                    UnknownBlockId
                );

            Assert.IsFalse(
                fluid.IsValid
            );
        }

        [Test]
        public void GetByBlockIdReturnsDefaultForAir()
        {
            FluidRuntimeData fluid =
                database.GetByBlockId(
                    BlockIds.Air
                );

            Assert.IsFalse(
                fluid.IsValid
            );
        }

        [Test]
        public void BlockLookupLengthIncludesHighestBlockId()
        {
            Assert.AreEqual(
                LavaBlockId + 1,
                database.BlockLookupLength
            );
        }

        [Test]
        public void TryGetByFluidTypeFindsWater()
        {
            bool found =
                database.TryGet(
                    FluidType.Water,
                    out FluidRuntimeData fluid
                );

            Assert.IsTrue(found);

            Assert.AreEqual(
                WaterBlockId,
                fluid.BlockId
            );
        }

        [Test]
        public void TryGetByFluidTypeFindsLava()
        {
            bool found =
                database.TryGet(
                    FluidType.Lava,
                    out FluidRuntimeData fluid
                );

            Assert.IsTrue(found);

            Assert.AreEqual(
                LavaBlockId,
                fluid.BlockId
            );
        }

        [Test]
        public void TryGetUnknownFluidTypeReturnsFalse()
        {
            bool found =
                database.TryGet(
                    FluidType.None,
                    out FluidRuntimeData fluid
                );

            Assert.IsFalse(found);
            Assert.IsFalse(fluid.IsValid);
        }

        [Test]
        public void NativeBlockLookupIsCreated()
        {
            NativeArray<FluidRuntimeData> lookup =
                database.AsBlockLookupNativeArray();

            Assert.IsTrue(
                lookup.IsCreated
            );

            Assert.AreEqual(
                LavaBlockId + 1,
                lookup.Length
            );
        }

        [Test]
        public void NativeBlockLookupContainsWater()
        {
            NativeArray<FluidRuntimeData> lookup =
                database.AsBlockLookupNativeArray();

            FluidRuntimeData fluid =
                lookup[WaterBlockId];

            Assert.AreEqual(
                FluidType.Water,
                fluid.Type
            );

            Assert.AreEqual(
                WaterBlockId,
                fluid.BlockId
            );

            Assert.IsTrue(
                fluid.IsValid
            );
        }

        [Test]
        public void NativeBlockLookupContainsLava()
        {
            NativeArray<FluidRuntimeData> lookup =
                database.AsBlockLookupNativeArray();

            FluidRuntimeData fluid =
                lookup[LavaBlockId];

            Assert.AreEqual(
                FluidType.Lava,
                fluid.Type
            );

            Assert.AreEqual(
                LavaBlockId,
                fluid.BlockId
            );

            Assert.IsTrue(
                fluid.IsValid
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
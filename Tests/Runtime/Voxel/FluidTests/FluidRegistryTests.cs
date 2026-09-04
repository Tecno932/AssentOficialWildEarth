using NUnit.Framework;
using Unity.Collections;
using WildEarth.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class FluidRegistryTests
    {
        [Test]
        public void RuntimeDataCanBeCreated()
        {
            FluidRuntimeData data =
                new FluidRuntimeData
                {
                    Type = FluidType.Water,
                    BlockId = 10,
                    MaxLevel = 15,
                    HorizontalFlowDecay = 1,
                    VerticalFlowDecay = 0,
                    IsLava = false
                };

            Assert.IsTrue(
                data.IsValid
            );
        }

        [Test]
        public void RuntimeDataPropertiesArePreserved()
        {
            FluidRuntimeData data =
                new FluidRuntimeData
                {
                    Type = FluidType.Lava,
                    BlockId = 11,
                    MaxLevel = 15,
                    HorizontalFlowDecay = 2,
                    VerticalFlowDecay = 0,
                    IsLava = true
                };

            Assert.AreEqual(
                FluidType.Lava,
                data.Type
            );

            Assert.AreEqual(
                11,
                data.BlockId
            );

            Assert.AreEqual(
                15,
                data.MaxLevel
            );

            Assert.AreEqual(
                2,
                data.HorizontalFlowDecay
            );

            Assert.AreEqual(
                0,
                data.VerticalFlowDecay
            );

            Assert.IsTrue(
                data.IsLava
            );

            Assert.IsTrue(
                data.IsValid
            );
        }

        [Test]
        public void NativeArrayCanStoreRuntimeData()
        {
            NativeArray<FluidRuntimeData> data =
                new NativeArray<FluidRuntimeData>(
                    2,
                    Allocator.Temp
                );

            try
            {
                data[0] =
                    new FluidRuntimeData
                    {
                        Type = FluidType.Water,
                        BlockId = 10,
                        MaxLevel = 15
                    };

                data[1] =
                    new FluidRuntimeData
                    {
                        Type = FluidType.Lava,
                        BlockId = 11,
                        MaxLevel = 15,
                        IsLava = true
                    };

                Assert.AreEqual(
                    2,
                    data.Length
                );

                Assert.AreEqual(
                    FluidType.Water,
                    data[0].Type
                );

                Assert.AreEqual(
                    FluidType.Lava,
                    data[1].Type
                );
            }
            finally
            {
                if (data.IsCreated)
                    data.Dispose();
            }
        }

        [Test]
        public void DifferentFluidTypesAreDistinct()
        {
            Assert.AreNotEqual(
                FluidType.None,
                FluidType.Water
            );

            Assert.AreNotEqual(
                FluidType.Water,
                FluidType.Lava
            );

            Assert.AreNotEqual(
                FluidType.None,
                FluidType.Lava
            );
        }
    }
}
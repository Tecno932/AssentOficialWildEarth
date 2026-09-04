using NUnit.Framework;
using WildEarth.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class FluidDefinitionTests
    {
        [Test]
        public void EmptyStateIsEmpty()
        {
            FluidState state = FluidState.Empty;

            Assert.IsTrue(state.IsEmpty);
            Assert.AreEqual(FluidType.None, state.Type);
            Assert.AreEqual(0, state.Level);
        }

        [Test]
        public void SourceHasMaximumLevel()
        {
            FluidState state =
                FluidState.Source(FluidType.Water);

            Assert.IsFalse(state.IsEmpty);
            Assert.IsTrue(state.IsSource);
            Assert.AreEqual(FluidType.Water, state.Type);
            Assert.AreEqual(
                FluidState.MaxLevel,
                state.Level
            );
        }

        [Test]
        public void ZeroLevelRemovesFluidType()
        {
            FluidState state =
                new FluidState(
                    FluidType.Water,
                    0
                );

            Assert.IsTrue(state.IsEmpty);
            Assert.AreEqual(FluidType.None, state.Type);
            Assert.AreEqual(0, state.Level);
        }

        [Test]
        public void LevelIsClampedToMaximum()
        {
            FluidState state =
                new FluidState(
                    FluidType.Water,
                    255
                );

            Assert.AreEqual(
                FluidState.MaxLevel,
                state.Level
            );
        }

        [Test]
        public void EmptySourceIsEmpty()
        {
            FluidState state =
                FluidState.Source(
                    FluidType.None
                );

            Assert.IsTrue(state.IsEmpty);
        }

        [Test]
        public void WithLevelChangesOnlyLevel()
        {
            FluidState source =
                FluidState.Source(
                    FluidType.Water
                );

            FluidState reduced =
                source.WithLevel(7);

            Assert.AreEqual(
                FluidType.Water,
                reduced.Type
            );

            Assert.AreEqual(
                7,
                reduced.Level
            );

            Assert.IsFalse(
                reduced.IsSource
            );
        }

        [Test]
        public void EqualStatesAreEqual()
        {
            FluidState first =
                new FluidState(
                    FluidType.Water,
                    8
                );

            FluidState second =
                new FluidState(
                    FluidType.Water,
                    8
                );

            Assert.AreEqual(
                first,
                second
            );

            Assert.IsTrue(
                first == second
            );

            Assert.IsFalse(
                first != second
            );
        }

        [Test]
        public void DifferentLevelsAreNotEqual()
        {
            FluidState first =
                new FluidState(
                    FluidType.Water,
                    8
                );

            FluidState second =
                new FluidState(
                    FluidType.Water,
                    7
                );

            Assert.AreNotEqual(
                first,
                second
            );
        }

        [Test]
        public void RuntimeDataCanBeValid()
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
        public void RuntimeDataWithAirIsInvalid()
        {
            FluidRuntimeData data =
                new FluidRuntimeData
                {
                    Type = FluidType.Water,
                    BlockId = BlockIds.Air,
                    MaxLevel = 15
                };

            Assert.IsFalse(
                data.IsValid
            );
        }

        [Test]
        public void RuntimeDataWithNoneIsInvalid()
        {
            FluidRuntimeData data =
                new FluidRuntimeData
                {
                    Type = FluidType.None,
                    BlockId = 10,
                    MaxLevel = 15
                };

            Assert.IsFalse(
                data.IsValid
            );
        }

        [Test]
        public void RuntimeDataWithZeroLevelIsInvalid()
        {
            FluidRuntimeData data =
                new FluidRuntimeData
                {
                    Type = FluidType.Water,
                    BlockId = 10,
                    MaxLevel = 0
                };

            Assert.IsFalse(
                data.IsValid
            );
        }
    }
}
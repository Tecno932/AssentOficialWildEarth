using NUnit.Framework;
using WildEarth.Voxel;

namespace WildEarth.Tests.Voxel
{
    public sealed class FluidPropagationTests
    {
        [Test]
        public void HorizontalFlowReducesLevel()
        {
            byte result =
                FluidPropagation.CalculateHorizontalLevel(
                    15,
                    1
                );

            Assert.AreEqual(
                14,
                result
            );
        }

        [Test]
        public void HorizontalFlowCannotGoBelowZero()
        {
            byte result =
                FluidPropagation.CalculateHorizontalLevel(
                    3,
                    3
                );

            Assert.AreEqual(
                0,
                result
            );
        }

        [Test]
        public void HorizontalFlowWithLargeDecayIsEmpty()
        {
            byte result =
                FluidPropagation.CalculateHorizontalLevel(
                    3,
                    10
                );

            Assert.AreEqual(
                0,
                result
            );
        }

        [Test]
        public void VerticalFlowReducesLevel()
        {
            byte result =
                FluidPropagation.CalculateVerticalLevel(
                    15,
                    1
                );

            Assert.AreEqual(
                14,
                result
            );
        }

        [Test]
        public void EmptySourceProducesEmptyFlow()
        {
            byte result =
                FluidPropagation.CalculateHorizontalLevel(
                    0,
                    1
                );

            Assert.AreEqual(
                0,
                result
            );
        }

        [Test]
        public void EmptyIncomingFluidCannotReplace()
        {
            FluidState current =
                new FluidState(
                    FluidType.Water,
                    10
                );

            FluidState incoming =
                FluidState.Empty;

            Assert.IsFalse(
                FluidPropagation.ShouldReplace(
                    current,
                    incoming
                )
            );
        }

        [Test]
        public void EmptyVoxelAcceptsFluid()
        {
            FluidState current =
                FluidState.Empty;

            FluidState incoming =
                new FluidState(
                    FluidType.Water,
                    10
                );

            Assert.IsTrue(
                FluidPropagation.ShouldReplace(
                    current,
                    incoming
                )
            );
        }

        [Test]
        public void StrongerSameFluidReplacesWeakerFluid()
        {
            FluidState current =
                new FluidState(
                    FluidType.Water,
                    5
                );

            FluidState incoming =
                new FluidState(
                    FluidType.Water,
                    10
                );

            Assert.IsTrue(
                FluidPropagation.ShouldReplace(
                    current,
                    incoming
                )
            );
        }

        [Test]
        public void WeakerSameFluidDoesNotReplaceStrongerFluid()
        {
            FluidState current =
                new FluidState(
                    FluidType.Water,
                    10
                );

            FluidState incoming =
                new FluidState(
                    FluidType.Water,
                    5
                );

            Assert.IsFalse(
                FluidPropagation.ShouldReplace(
                    current,
                    incoming
                )
            );
        }

        [Test]
        public void DifferentFluidTypesDoNotReplaceEachOther()
        {
            FluidState current =
                new FluidState(
                    FluidType.Water,
                    10
                );

            FluidState incoming =
                new FluidState(
                    FluidType.Lava,
                    15
                );

            Assert.IsFalse(
                FluidPropagation.ShouldReplace(
                    current,
                    incoming
                )
            );
        }

        [Test]
        public void SourceHasMaximumLevel()
        {
            FluidState source =
                FluidState.Source(
                    FluidType.Water
                );

            Assert.AreEqual(
                FluidState.MaxLevel,
                source.Level
            );

            Assert.IsTrue(
                source.IsSource
            );
        }

        [Test]
        public void SimulationStateAcceptsValidLocalCoordinates()
        {
            FluidSimulationState state =
                new FluidSimulationState(
                    0,
                    5,
                    15,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            Assert.IsTrue(
                state.IsValid
            );

            Assert.IsFalse(
                state.IsEmpty
            );
        }

        [Test]
        public void SimulationStateRejectsInvalidCoordinates()
        {
            FluidSimulationState state =
                new FluidSimulationState(
                    -1,
                    5,
                    5,
                    FluidState.Source(
                        FluidType.Water
                    )
                );

            Assert.IsFalse(
                state.IsValid
            );
        }

        [Test]
        public void DefaultSimulationSettingsAreValid()
        {
            FluidSimulationSettings settings =
                FluidSimulationSettings.Default;

            Assert.Greater(
                settings.MaxUpdatesPerTick,
                0
            );

            Assert.Greater(
                settings.MaxPropagationDistance,
                0
            );

            Assert.Greater(
                settings.TicksPerSecond,
                0
            );

            Assert.IsTrue(
                settings.AllowHorizontalFlow
            );

            Assert.IsTrue(
                settings.AllowVerticalFlow
            );
        }
    }
}
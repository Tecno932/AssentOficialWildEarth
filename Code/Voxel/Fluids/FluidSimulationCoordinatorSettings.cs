using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct FluidSimulationCoordinatorSettings
    {
        [UnityEngine.Min(1)]
        public int MaxConcurrentSimulations;

        public static FluidSimulationCoordinatorSettings Default =>
            new FluidSimulationCoordinatorSettings
            {
                MaxConcurrentSimulations = 4
            };
    }
}
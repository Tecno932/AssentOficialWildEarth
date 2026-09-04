using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct FluidSimulationSettings
    {
        [UnityEngine.Min(1)]
        public int MaxUpdatesPerTick;

        [UnityEngine.Min(1)]
        public int MaxPropagationDistance;

        [UnityEngine.Min(0)]
        public int HorizontalDecay;

        [UnityEngine.Min(0)]
        public int VerticalDecay;

        [UnityEngine.Min(1)]
        public int TicksPerSecond;

        public bool AllowHorizontalFlow;

        public bool AllowVerticalFlow;

        public static FluidSimulationSettings Default =>
            new FluidSimulationSettings
            {
                MaxUpdatesPerTick = 4096,
                MaxPropagationDistance = 16,
                HorizontalDecay = 1,
                VerticalDecay = 0,
                TicksPerSecond = 10,
                AllowHorizontalFlow = true,
                AllowVerticalFlow = true
            };
    }
}
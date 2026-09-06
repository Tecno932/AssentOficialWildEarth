using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct FluidGenerationSettings
    {
        public bool Enabled;

        public bool GenerateWater;

        public static FluidGenerationSettings Default =>
            new FluidGenerationSettings
            {
                Enabled = true,
                GenerateWater = true
            };
    }
}
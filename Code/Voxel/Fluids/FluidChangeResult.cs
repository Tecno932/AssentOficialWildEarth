using System;

namespace WildEarth.Voxel
{
    [Serializable]
    public struct FluidChangeResult
    {
        public FluidChange Change;

        public bool Applied;

        public bool TargetChunkLoaded;

        public bool TargetWasAir;

        public bool ReplacedWeakerFluid;

        public FluidChangeResult(
            FluidChange change,
            bool applied,
            bool targetChunkLoaded,
            bool targetWasAir,
            bool replacedWeakerFluid)
        {
            Change = change;
            Applied = applied;
            TargetChunkLoaded = targetChunkLoaded;
            TargetWasAir = targetWasAir;
            ReplacedWeakerFluid =
                replacedWeakerFluid;
        }
    }
}
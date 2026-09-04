namespace WildEarth.Voxel
{
    public static class FluidPropagation
    {
        public static byte CalculateHorizontalLevel(
            byte sourceLevel,
            byte decay)
        {
            if (sourceLevel == 0)
                return 0;

            if (decay >= sourceLevel)
                return 0;

            return (byte)(
                sourceLevel - decay
            );
        }

        public static byte CalculateVerticalLevel(
            byte sourceLevel,
            byte decay)
        {
            if (sourceLevel == 0)
                return 0;

            if (decay >= sourceLevel)
                return 0;

            return (byte)(
                sourceLevel - decay
            );
        }

        public static bool ShouldReplace(
            FluidState current,
            FluidState incoming)
        {
            if (incoming.IsEmpty)
                return false;

            if (current.IsEmpty)
                return true;

            if (current.Type != incoming.Type)
                return false;

            return incoming.Level >
                   current.Level;
        }
    }
}